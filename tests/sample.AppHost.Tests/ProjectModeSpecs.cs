using System.Net;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Xunit;

namespace sample.AppHost.Tests;

public class ProjectModeFixture : IAsyncLifetime
{
    private static readonly string GlobalConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".aspire-e2e",
        "resources.json");

    public IDistributedApplicationTestingBuilder Builder { get; private set; } = null!;
    public DistributedApplication App { get; private set; } = null!;

    private string? _originalConfig;

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

        Builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.sample_AppHost>();

        App = await Builder.BuildAsync();
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await App.StartAsync(cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
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

    private static string GetApiServiceProjectPath()
    {
        var testDir = AppContext.BaseDirectory;
        var sampleDir = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", "..", "sample", "sample.ApiService"));
        return Path.Combine(sampleDir, "sample.ApiService.csproj");
    }
}

public class ProjectModeSpecs : IClassFixture<ProjectModeFixture>
{
    private readonly ProjectModeFixture _fixture;

    public ProjectModeSpecs(ProjectModeFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void ApiServiceResourceIsProject()
    {
        var resource = _fixture.Builder.Resources.Single(r => r.Name == "sample-apiservice");
        Assert.IsAssignableFrom<ProjectResource>(resource);
    }

    [Fact]
    public async Task ApiServiceHealthEndpointReturnsOk()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await _fixture.App.ResourceNotifications
            .WaitForResourceHealthyAsync("sample-apiservice", cts.Token);

        var httpClient = _fixture.App.CreateHttpClient("sample-apiservice");
        var response = await httpClient.GetAsync("/health", cts.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApiServiceRootEndpointReturnsContent()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await _fixture.App.ResourceNotifications
            .WaitForResourceHealthyAsync("sample-apiservice", cts.Token);

        var httpClient = _fixture.App.CreateHttpClient("sample-apiservice");
        var response = await httpClient.GetAsync("/", cts.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync(cts.Token);
        Assert.Contains("API service is running", content);
    }
}
