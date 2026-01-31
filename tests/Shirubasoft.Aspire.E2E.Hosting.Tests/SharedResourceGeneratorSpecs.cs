using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Shirubasoft.Aspire.E2E.Hosting.Generator;
using Xunit;

namespace Shirubasoft.Aspire.E2E.Hosting.Tests;

public class SingleResourceGenerationSpecs
{
    [Fact]
    public void Generates_typed_resource_builder_for_single_resource()
    {
        var json = """
        [
          { "Id": "apiservice", "Name": "ApiService", "Mode": "", "ProjectPath": "", "ContainerImage": "", "ContainerTag": "", "BuildImageCommand": "", "BuildImage": "false" }
        ]
        """;

        var generatedSource = RunGenerator(json);

        Assert.Single(generatedSource);
        Assert.Contains("class ApiServiceResourceBuilder", generatedSource[0]);
        Assert.Contains("interface IApiServiceResourceBuilder", generatedSource[0]);
        Assert.Contains("AddApiService", generatedSource[0]);
        Assert.Contains("Aspire:Resources:apiservice:Mode", generatedSource[0]);
    }

    [Fact]
    public void Generates_default_name_from_id_when_name_is_empty()
    {
        var json = """
        [
          { "Id": "payments-service", "Name": "", "Mode": "", "ProjectPath": "", "ContainerImage": "", "ContainerTag": "", "BuildImageCommand": "", "BuildImage": "false" }
        ]
        """;

        var generatedSource = RunGenerator(json);

        Assert.Single(generatedSource);
        Assert.Contains("class PaymentsServiceResourceBuilder", generatedSource[0]);
        Assert.Contains("AddPaymentsService", generatedSource[0]);
    }

    private static List<string> RunGenerator(string json)
    {
        var additionalText = new InMemoryAdditionalText("SharedResourceReferences.g.json", json);

        var compilation = CSharpCompilation.Create("TestAssembly",
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new SharedResourceGenerator())
            .AddAdditionalTexts([additionalText]);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, TestContext.Current.CancellationToken);

        var results = driver.GetRunResult();
        return results.GeneratedTrees
            .Select(t => t.GetText(TestContext.Current.CancellationToken).ToString())
            .ToList();
    }
}

public class ImageRegistryGenerationSpecs
{
    [Fact]
    public void Generates_resource_when_ImageRegistry_field_is_present()
    {
        var json = """
        [
          { "Id": "apiservice", "Name": "ApiService", "Mode": "Container", "ProjectPath": "", "ContainerImage": "ghcr.io/myorg/apiservice", "ContainerTag": "latest", "BuildImageCommand": "", "BuildImage": "false", "ImageRegistry": "ghcr.io/myorg" }
        ]
        """;

        var additionalText = new InMemoryAdditionalText("SharedResourceReferences.g.json", json);

        var compilation = CSharpCompilation.Create("TestAssembly",
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new SharedResourceGenerator())
            .AddAdditionalTexts([additionalText]);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, TestContext.Current.CancellationToken);

        var results = driver.GetRunResult();
        var sources = results.GeneratedTrees
            .Select(t => t.GetText(TestContext.Current.CancellationToken).ToString())
            .ToList();

        Assert.Single(sources);
        Assert.Contains("AddApiService", sources[0]);
    }

    [Fact]
    public void Generates_resource_when_ImageRegistry_field_is_missing()
    {
        // Backwards compatibility: JSON without ImageRegistry should still work
        var json = """
        [
          { "Id": "apiservice", "Name": "ApiService", "Mode": "", "ProjectPath": "", "ContainerImage": "", "ContainerTag": "", "BuildImageCommand": "", "BuildImage": "false" }
        ]
        """;

        var additionalText = new InMemoryAdditionalText("SharedResourceReferences.g.json", json);

        var compilation = CSharpCompilation.Create("TestAssembly",
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new SharedResourceGenerator())
            .AddAdditionalTexts([additionalText]);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, TestContext.Current.CancellationToken);

        var results = driver.GetRunResult();
        Assert.Single(results.GeneratedTrees);
    }
}

public class EmptyResourceGenerationSpecs
{
    [Fact]
    public void Generates_nothing_for_empty_json()
    {
        var json = "[]";
        var additionalText = new InMemoryAdditionalText("SharedResourceReferences.g.json", json);

        var compilation = CSharpCompilation.Create("TestAssembly",
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new SharedResourceGenerator())
            .AddAdditionalTexts([additionalText]);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, TestContext.Current.CancellationToken);

        var results = driver.GetRunResult();
        Assert.Empty(results.GeneratedTrees);
    }
}

public class MultipleResourceGenerationSpecs
{
    [Fact]
    public void Generates_one_source_per_resource()
    {
        var json = """
        [
          { "Id": "apiservice", "Name": "ApiService", "Mode": "", "ProjectPath": "" },
          { "Id": "webfrontend", "Name": "WebFrontend", "Mode": "", "ProjectPath": "" }
        ]
        """;

        var generatedSource = RunGenerator(json);

        Assert.Equal(2, generatedSource.Count);
        Assert.Contains(generatedSource, s => s.Contains("AddApiService"));
        Assert.Contains(generatedSource, s => s.Contains("AddWebFrontend"));
    }

    private static List<string> RunGenerator(string json)
    {
        var additionalText = new InMemoryAdditionalText("SharedResourceReferences.g.json", json);

        var compilation = CSharpCompilation.Create("TestAssembly",
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new SharedResourceGenerator())
            .AddAdditionalTexts([additionalText]);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, TestContext.Current.CancellationToken);

        return driver.GetRunResult().GeneratedTrees
            .Select(t => t.GetText(TestContext.Current.CancellationToken).ToString())
            .ToList();
    }
}

public class ProjectModeGenerationSpecs
{
    [Fact]
    public void Generates_project_metadata_class_for_project_mode()
    {
        var json = """
        [
          { "Id": "apiservice", "Name": "ApiService", "Mode": "Project", "ProjectPath": "/src/ApiService/ApiService.csproj" }
        ]
        """;

        var generatedSource = RunGenerator(json);

        Assert.Single(generatedSource);
        Assert.Contains("class ApiServiceProjectMetadata", generatedSource[0]);
        Assert.Contains("IProjectMetadata", generatedSource[0]);
        Assert.Contains("/src/ApiService/ApiService.csproj", generatedSource[0]);
    }

    [Fact]
    public void Does_not_generate_project_metadata_when_mode_is_container()
    {
        var json = """
        [
          { "Id": "apiservice", "Name": "ApiService", "Mode": "Container", "ProjectPath": "" }
        ]
        """;

        var generatedSource = RunGenerator(json);

        Assert.Single(generatedSource);
        Assert.DoesNotContain("ProjectMetadata", generatedSource[0]);
    }

    private static List<string> RunGenerator(string json)
    {
        var additionalText = new InMemoryAdditionalText("SharedResourceReferences.g.json", json);

        var compilation = CSharpCompilation.Create("TestAssembly",
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new SharedResourceGenerator())
            .AddAdditionalTexts([additionalText]);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, TestContext.Current.CancellationToken);

        return driver.GetRunResult().GeneratedTrees
            .Select(t => t.GetText(TestContext.Current.CancellationToken).ToString())
            .ToList();
    }
}

public class JsonEscapingSpecs
{
    [Fact]
    public void Handles_backslashes_in_project_path()
    {
        var json = """
        [
          { "Id": "apiservice", "Name": "ApiService", "Mode": "Project", "ProjectPath": "C:\\src\\ApiService\\ApiService.csproj" }
        ]
        """;

        var generatedSource = RunGenerator(json);

        Assert.Single(generatedSource);
        Assert.Contains("ApiService", generatedSource[0]);
    }

    [Fact]
    public void Skips_entries_with_missing_id()
    {
        var json = """
        [
          { "Id": "", "Name": "NoId", "Mode": "", "ProjectPath": "" },
          { "Id": "valid", "Name": "Valid", "Mode": "", "ProjectPath": "" }
        ]
        """;

        var generatedSource = RunGenerator(json);

        Assert.Single(generatedSource);
        Assert.Contains("AddValid", generatedSource[0]);
    }

    private static List<string> RunGenerator(string json)
    {
        var additionalText = new InMemoryAdditionalText("SharedResourceReferences.g.json", json);

        var compilation = CSharpCompilation.Create("TestAssembly",
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new SharedResourceGenerator())
            .AddAdditionalTexts([additionalText]);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, TestContext.Current.CancellationToken);

        return driver.GetRunResult().GeneratedTrees
            .Select(t => t.GetText(TestContext.Current.CancellationToken).ToString())
            .ToList();
    }
}

internal sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
{
    public override string Path => path;

    public override SourceText? GetText(CancellationToken cancellationToken = default) =>
        SourceText.From(text);
}
