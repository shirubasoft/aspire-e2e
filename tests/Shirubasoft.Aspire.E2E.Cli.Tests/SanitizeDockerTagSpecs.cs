using Shirubasoft.Aspire.E2E.Cli.Commands;
using Xunit;

namespace Shirubasoft.Aspire.E2E.Cli.Tests;

public class SanitizeDockerTagSpecs
{
    [Fact]
    public void SimpleBranchName_ReturnsUnchanged()
    {
        Assert.Equal("master", BuildCommand.SanitizeDockerTag("master"));
    }

    [Fact]
    public void BranchWithSlashes_ReplacesWithDashes()
    {
        Assert.Equal("fix-package-generator", BuildCommand.SanitizeDockerTag("fix/package-generator"));
    }

    [Fact]
    public void BranchWithMultipleSlashes_ReplacesAll()
    {
        Assert.Equal("feature-scope-name", BuildCommand.SanitizeDockerTag("feature/scope/name"));
    }

    [Fact]
    public void DotsAndUnderscores_ArePreserved()
    {
        Assert.Equal("release_1.0", BuildCommand.SanitizeDockerTag("release_1.0"));
    }
}
