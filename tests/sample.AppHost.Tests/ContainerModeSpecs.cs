using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Xunit;

namespace sample.AppHost.Tests;

public class ContainerModeFixture : IAsyncLifetime
{
    private const string ContainerImage = "sample-apiservice";
    private const string ContainerTag = "latest";

    private static readonly string GlobalConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".aspire-e2e",
        "resources.json");

    public IDistributedApplicationTestingBuilder Builder { get; private set; } = null!;
    public DistributedApplication App { get; private set; } = null!;
    public bool ImageAvailable { get; private set; }

    private string? _originalConfig;

    public async ValueTask InitializeAsync()
    {
        ImageAvailable = await IsDockerImageAvailableAsync();
        if (!ImageAvailable)
        {
            return;
        }

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

        Builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.sample_AppHost>();

        App = await Builder.BuildAsync();
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await App.StartAsync(cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (!ImageAvailable)
        {
            return;
        }

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await App.StopAsync(cts.Token);
        await App.DisposeAsync();

        if (_originalConfig is not null)
        {
            await File.WriteAllTextAsync(GlobalConfigPath, _originalConfig);
        }
        else if (File.Exists(GlobalConfigPath))
        {
            File.Delete(GlobalConfigPath);
        }
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
}

public class ContainerModeSpecs : IClassFixture<ContainerModeFixture>
{
    private readonly ContainerModeFixture _fixture;

    public ContainerModeSpecs(ContainerModeFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void ApiServiceResourceIsContainer()
    {
        Assert.SkipUnless(_fixture.ImageAvailable, "Container image not available. Build it first.");

        var resource = _fixture.Builder.Resources.Single(r => r.Name == "sample-apiservice");
        Assert.IsAssignableFrom<ContainerResource>(resource);
    }

    [Fact]
    public async Task ApiServiceHealthEndpointReturnsOk()
    {
        Assert.SkipUnless(_fixture.ImageAvailable, "Container image not available. Build it first.");

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await _fixture.App.ResourceNotifications
            .WaitForResourceHealthyAsync("sample-apiservice", cts.Token);

        var httpClient = _fixture.App.CreateHttpClient("sample-apiservice");
        var response = await httpClient.GetAsync("/health", cts.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
