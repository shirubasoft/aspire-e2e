using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Xunit;

namespace sample.AppHost.Tests;

public class ContainerModeSpecs : IAsyncDisposable
{
    private const string ContainerImage = "sample-apiservice";
    private const string ContainerTag = "latest";

    private static readonly string GlobalConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".aspire-e2e",
        "resources.json");

    private IDistributedApplicationTestingBuilder? _builder;
    private DistributedApplication? _app;
    private string? _originalConfig;

    private async Task<(IDistributedApplicationTestingBuilder builder, DistributedApplication app)> StartAppAsync()
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
                        ContainerImage,
                        ContainerTag
                    }
                }
            }
        };

        Directory.CreateDirectory(Path.GetDirectoryName(GlobalConfigPath)!);
        await File.WriteAllTextAsync(GlobalConfigPath, JsonSerializer.Serialize(testConfig));

        _builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.sample_AppHost>();

        _app = await _builder.BuildAsync();
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await _app.StartAsync(cts.Token);
        return (_builder, _app);
    }

    [Fact]
    public async Task ApiServiceResourceIsContainer()
    {
        Assert.SkipUnless(await IsDockerImageAvailableAsync(), "Container image not available. Build it first.");

        var (builder, _) = await StartAppAsync();

        var resource = builder.Resources.Single(r => r.Name == "sample-apiservice");
        Assert.IsAssignableFrom<ContainerResource>(resource);
    }

    [Fact]
    public async Task ApiServiceHealthEndpointReturnsOk()
    {
        Assert.SkipUnless(await IsDockerImageAvailableAsync(), "Container image not available. Build it first.");

        var (_, app) = await StartAppAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("sample-apiservice", cts.Token);

        var httpClient = app.CreateHttpClient("sample-apiservice");
        var response = await httpClient.GetAsync("/health", cts.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<bool> IsDockerImageAvailableAsync()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"image inspect {ContainerImage}:{ContainerTag}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });

            if (process is null)
                return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await _app.StopAsync(cts.Token);
            await _app.DisposeAsync();
        }

        if (_originalConfig is not null)
        {
            await File.WriteAllTextAsync(GlobalConfigPath, _originalConfig);
        }
        else if (File.Exists(GlobalConfigPath))
        {
            File.Delete(GlobalConfigPath);
        }
    }
}
