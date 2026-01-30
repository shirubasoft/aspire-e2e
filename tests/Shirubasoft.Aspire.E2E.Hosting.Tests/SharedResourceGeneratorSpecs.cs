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

internal sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
{
    public override string Path => path;

    public override SourceText? GetText(CancellationToken cancellationToken = default) =>
        SourceText.From(text);
}
