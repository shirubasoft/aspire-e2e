using Xunit;

namespace Shirubasoft.Aspire.E2E.Hosting.Tests.MSBuildTargets;

public class ContainerModeBuildSpecs : IDisposable
{
    private readonly MsBuildTestFixture _fixture = new();

    [Fact]
    public void Invokes_build_for_container_mode_resource()
    {
        _fixture.WriteFakeCli(new Dictionary<string, (string, int)>
        {
            ["get-mode myapi"] = ("Container", 0),
            ["get-config myapi SkipImageBuild"] = ("False", 0),
            ["build myapi"] = ("image-built", 0),
        });

        _fixture.WriteTestProject([
            """<SharedResourceReference Include="myapi" ContainerImage="myorg/myapi" ContainerTag="latest" />""",
        ]);

        var (output, exitCode) = _fixture.RunMsBuildTarget("_BuildSharedResourceContainerImages");

        Assert.True(exitCode == 0, $"MSBuild failed (exit {exitCode}):\n{output}");
        Assert.Contains("Building container image for 'myapi'", output);
        Assert.Contains("image-built", output);
    }

    public void Dispose() => _fixture.Dispose();
}

public class SkipImageBuildSpecs : IDisposable
{
    private readonly MsBuildTestFixture _fixture = new();

    [Fact]
    public void Skips_build_when_SkipImageBuild_is_true()
    {
        _fixture.WriteFakeCli(new Dictionary<string, (string, int)>
        {
            ["get-mode myapi"] = ("Container", 0),
            ["get-config myapi SkipImageBuild"] = ("True", 0),
        });

        _fixture.WriteTestProject([
            """<SharedResourceReference Include="myapi" ContainerImage="myorg/myapi" ContainerTag="latest" />""",
        ]);

        var (output, exitCode) = _fixture.RunMsBuildTarget("_BuildSharedResourceContainerImages");

        Assert.True(exitCode == 0, $"MSBuild failed (exit {exitCode}):\n{output}");
        Assert.Contains("Skipping image build for 'myapi'", output);
        Assert.DoesNotContain("Building container image", output);
    }

    public void Dispose() => _fixture.Dispose();
}

public class ProjectModeNoBuildSpecs : IDisposable
{
    private readonly MsBuildTestFixture _fixture = new();

    [Fact]
    public void Does_not_build_image_for_project_mode_resource()
    {
        _fixture.WriteFakeCli(new Dictionary<string, (string, int)>
        {
            ["get-mode myapi"] = ("Project", 0),
            ["get-project-path myapi"] = ("/some/path/MyApi.csproj", 0),
        });

        _fixture.WriteTestProject([
            """<SharedResourceReference Include="myapi" />""",
        ]);

        var (output, exitCode) = _fixture.RunMsBuildTarget("_BuildSharedResourceContainerImages");

        Assert.True(exitCode == 0, $"MSBuild failed (exit {exitCode}):\n{output}");
        Assert.DoesNotContain("Building container image", output);
    }

    public void Dispose() => _fixture.Dispose();
}
