using System.Text.Json;
using Xunit;

namespace Shirubasoft.Aspire.E2E.Hosting.Tests.MSBuildTargets;

public class SingleResourceSerializationSpecs : IDisposable
{
    private readonly MsBuildTestFixture _fixture = new();

    [Fact]
    public void Produces_valid_json_with_resource_fields()
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

        // Find the generated JSON - MSBuild may nest under TFM subfolder
        var jsonFiles = Directory.GetFiles(_fixture.IntermediateOutputPath, "SharedResourceReferences.g.json", SearchOption.AllDirectories);
        Assert.True(jsonFiles.Length > 0, $"No JSON file found under {_fixture.IntermediateOutputPath}. Output:\n{output}");
        var jsonPath = jsonFiles[0];

        var json = File.ReadAllText(jsonPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(1, root.GetArrayLength());

        var resource = root[0];
        Assert.Equal("myapi", resource.GetProperty("Id").GetString());
        Assert.Equal("Container", resource.GetProperty("Mode").GetString());
        Assert.Equal("myorg/myapi", resource.GetProperty("ContainerImage").GetString());
        Assert.Equal("latest", resource.GetProperty("ContainerTag").GetString());
    }

    public void Dispose() => _fixture.Dispose();
}

public class MultipleResourceSerializationSpecs : IDisposable
{
    private readonly MsBuildTestFixture _fixture = new();

    [Fact]
    public void Produces_json_array_with_all_resources()
    {
        _fixture.WriteFakeCli(new Dictionary<string, (string, int)>
        {
            ["get-mode api-one"] = ("Container", 0),
            ["get-mode api-two"] = ("Container", 0),
        });

        _fixture.WriteTestProject([
            """<SharedResourceReference Include="api-one" ContainerImage="img1" ContainerTag="v1" />""",
            """<SharedResourceReference Include="api-two" ContainerImage="img2" ContainerTag="v2" />""",
        ]);

        var (output, exitCode) = _fixture.RunMsBuildTarget("_SerializeSharedResourceReferences");

        Assert.True(exitCode == 0, $"MSBuild failed (exit {exitCode}):\n{output}");

        var jsonFiles = Directory.GetFiles(_fixture.IntermediateOutputPath, "SharedResourceReferences.g.json", SearchOption.AllDirectories);
        Assert.True(jsonFiles.Length > 0, $"No JSON file found under {_fixture.IntermediateOutputPath}");
        var json = File.ReadAllText(jsonFiles[0]);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(2, root.GetArrayLength());

        var ids = root.EnumerateArray()
            .Select(e => e.GetProperty("Id").GetString())
            .ToList();

        Assert.Contains("api-one", ids);
        Assert.Contains("api-two", ids);
    }

    public void Dispose() => _fixture.Dispose();
}
