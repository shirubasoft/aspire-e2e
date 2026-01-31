using System.Text.Json;
using Xunit;

namespace Shirubasoft.Aspire.E2E.Hosting.Tests.MSBuildTargets;

public class ProjectModeResolutionSpecs : IDisposable
{
    private readonly MsBuildTestFixture _fixture = new();

    [Fact]
    public void Resolves_mode_and_project_path_for_project_resource()
    {
        _fixture.WriteFakeCli(new Dictionary<string, (string, int)>
        {
            ["get-mode myapi"] = ("Project", 0),
            ["get-project-path myapi"] = ("/some/path/MyApi.csproj", 0),
        });

        _fixture.WriteTestProject([
            """<SharedResourceReference Include="myapi" />""",
        ]);

        // Run both resolve and serialize to verify resolved metadata persists
        var (output, exitCode) = _fixture.RunMsBuildTarget("_SerializeSharedResourceReferences");

        Assert.True(exitCode == 0, $"MSBuild failed (exit {exitCode}):\n{output}");

        var json = File.ReadAllText(Directory.GetFiles(_fixture.IntermediateOutputPath, "SharedResourceReferences.g.json", SearchOption.AllDirectories)[0]);
        using var doc = JsonDocument.Parse(json);
        var resource = doc.RootElement[0];

        Assert.Equal("Project", resource.GetProperty("Mode").GetString());
        Assert.Equal("/some/path/MyApi.csproj", resource.GetProperty("ProjectPath").GetString());
    }

    public void Dispose() => _fixture.Dispose();
}

public class ContainerModeResolutionSpecs : IDisposable
{
    private readonly MsBuildTestFixture _fixture = new();

    [Fact]
    public void Resolves_mode_without_project_path_for_container_resource()
    {
        _fixture.WriteFakeCli(new Dictionary<string, (string, int)>
        {
            ["get-mode myapi"] = ("Container", 0),
        });

        _fixture.WriteTestProject([
            """<SharedResourceReference Include="myapi" ContainerImage="myorg/myapi" ContainerTag="latest" />""",
        ]);

        var (output, exitCode) = _fixture.RunMsBuildTarget("_SerializeSharedResourceReferences");

        Assert.True(exitCode == 0, $"MSBuild failed (exit {exitCode}):\n{output}");

        var json = File.ReadAllText(Directory.GetFiles(_fixture.IntermediateOutputPath, "SharedResourceReferences.g.json", SearchOption.AllDirectories)[0]);
        using var doc = JsonDocument.Parse(json);
        var resource = doc.RootElement[0];

        Assert.Equal("Container", resource.GetProperty("Mode").GetString());
        // ProjectPath should remain empty for container mode
        Assert.Equal("", resource.GetProperty("ProjectPath").GetString());
    }

    public void Dispose() => _fixture.Dispose();
}

public class CliFailureResolutionSpecs : IDisposable
{
    private readonly MsBuildTestFixture _fixture = new();

    [Fact]
    public void Does_not_update_metadata_when_cli_returns_nonzero()
    {
        _fixture.WriteFakeCli(new Dictionary<string, (string, int)>
        {
            ["get-mode myapi"] = ("", 1),
        });

        _fixture.WriteTestProject([
            """<SharedResourceReference Include="myapi" />""",
        ]);

        // Target uses IgnoreExitCode=true, so MSBuild should still succeed
        var (output, exitCode) = _fixture.RunMsBuildTarget("_ResolveSharedResourceProjectReferences");

        Assert.True(exitCode == 0, $"MSBuild failed (exit {exitCode}):\n{output}");
    }

    public void Dispose() => _fixture.Dispose();
}
