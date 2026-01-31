using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Xunit;

namespace sample.AppHost.Tests;

public class ContainerModeSpecs : IAsyncLifetime, IDisposable
{
    private static readonly string GlobalConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".aspire-e2e",
        "resources.json");

    private string? _originalConfig;
    private IDistributedApplicationTestingBuilder _builder = null!;

    public async ValueTask InitializeAsync()
    {
        _originalConfig = File.Exists(GlobalConfigPath) ? await File.ReadAllTextAsync(GlobalConfigPath) : null;

        var testConfig = new
        {
            Aspire = new
            {
                Resources = new Dictionary<string, object>
                {
                    ["sample-apiservice"] = new
                    {
                        Mode = "Container",
                        ContainerImage = "sample-apiservice",
                        ContainerTag = "latest"
                    }
                }
            }
        };

        Directory.CreateDirectory(Path.GetDirectoryName(GlobalConfigPath)!);
        await File.WriteAllTextAsync(GlobalConfigPath, JsonSerializer.Serialize(testConfig));

        _builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.sample_AppHost>();
    }

    [Fact]
    public void ApiServiceIsRegisteredAsContainer()
    {
        var resource = _builder.Resources.Single(r => r.Name == "sample-apiservice");
        Assert.IsAssignableFrom<ContainerResource>(resource);
    }

    [Fact]
    public void WebFrontendIsRegistered()
    {
        var resource = _builder.Resources.Single(r => r.Name == "webfrontend");
        Assert.IsAssignableFrom<ProjectResource>(resource);
    }

    public void Dispose()
    {
        if (_originalConfig is not null)
        {
            File.WriteAllText(GlobalConfigPath, _originalConfig);
        }
        else if (File.Exists(GlobalConfigPath))
        {
            File.Delete(GlobalConfigPath);
        }
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
