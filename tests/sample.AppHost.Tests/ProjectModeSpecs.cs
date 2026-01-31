using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Xunit;

namespace sample.AppHost.Tests;

public class ProjectModeSpecs : IAsyncLifetime, IDisposable
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
                        Mode = "Project",
                        ProjectPath = GetApiServiceProjectPath()
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
    public void ApiServiceIsRegisteredAsProject()
    {
        var resource = _builder.Resources.Single(r => r.Name == "sample-apiservice");
        Assert.IsAssignableFrom<ProjectResource>(resource);
    }

    [Fact]
    public void WebFrontendIsRegistered()
    {
        var resource = _builder.Resources.Single(r => r.Name == "webfrontend");
        Assert.IsAssignableFrom<ProjectResource>(resource);
    }

    private static string GetApiServiceProjectPath()
    {
        var testDir = AppContext.BaseDirectory;
        var sampleDir = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", "..", "sample", "sample.ApiService"));
        return Path.Combine(sampleDir, "sample.ApiService.csproj");
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
