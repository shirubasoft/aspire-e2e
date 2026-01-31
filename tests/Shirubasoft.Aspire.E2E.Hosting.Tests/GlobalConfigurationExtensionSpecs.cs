using System.Text.Json;
using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Shirubasoft.Aspire.E2E.Common;
using Xunit;

namespace Shirubasoft.Aspire.E2E.Hosting.Tests;

public class GlobalConfigurationExtensionSpecs : IDisposable
{
    private readonly string _tempDir;

    public GlobalConfigurationExtensionSpecs()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"aspire-e2e-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private string WriteConfig(GlobalConfigFile config)
    {
        var path = Path.Combine(_tempDir, "resources.json");
        config.Save(path);
        return path;
    }

    [Fact]
    public void NoOverridesLeavesRawValuesIntact()
    {
        var config = new GlobalConfigFile();
        config.Aspire.Resources["svc"] = new ResourceEntry
        {
            Id = "svc",
            Mode = "Project",
            ContainerImage = "myapp",
            ContainerTag = "1.0",
            ImageRegistry = "docker.io"
        };

        var path = WriteConfig(config);

        var configuration = new ConfigurationBuilder()
            .AddGlobalResourceConfiguration(path)
            .Build();

        Assert.Equal("Project", configuration["Aspire:Resources:svc:Mode"]);
        Assert.Equal("myapp", configuration["Aspire:Resources:svc:ContainerImage"]);
        Assert.Equal("1.0", configuration["Aspire:Resources:svc:ContainerTag"]);
        Assert.Equal("docker.io", configuration["Aspire:Resources:svc:ImageRegistry"]);
    }

    [Fact]
    public void ModeOverrideAppliedToConfiguration()
    {
        var config = new GlobalConfigFile();
        config.Aspire.Resources["svc"] = new ResourceEntry
        {
            Id = "svc",
            Mode = "Project"
        };
        config.Aspire.Overrides = new OverrideSettings { Mode = "Container" };

        var path = WriteConfig(config);

        var configuration = new ConfigurationBuilder()
            .AddGlobalResourceConfiguration(path)
            .Build();

        Assert.Equal("Container", configuration["Aspire:Resources:svc:Mode"]);
    }

    [Fact]
    public void BuildImageOverrideAppliedToConfiguration()
    {
        var config = new GlobalConfigFile();
        config.Aspire.Resources["svc"] = new ResourceEntry
        {
            Id = "svc",
            BuildImage = false
        };
        config.Aspire.Overrides = new OverrideSettings { BuildImage = true };

        var path = WriteConfig(config);

        var configuration = new ConfigurationBuilder()
            .AddGlobalResourceConfiguration(path)
            .Build();

        Assert.Equal("True", configuration["Aspire:Resources:svc:BuildImage"]);
    }

    [Fact]
    public void ImageRegistryRewritesAppliedToConfiguration()
    {
        var config = new GlobalConfigFile();
        config.Aspire.Resources["svc"] = new ResourceEntry
        {
            Id = "svc",
            ImageRegistry = "docker.io"
        };
        config.Aspire.Overrides = new OverrideSettings
        {
            ImageRegistryRewrites = new Dictionary<string, string>
            {
                ["docker.io"] = "ghcr.io"
            }
        };

        var path = WriteConfig(config);

        var configuration = new ConfigurationBuilder()
            .AddGlobalResourceConfiguration(path)
            .Build();

        Assert.Equal("ghcr.io", configuration["Aspire:Resources:svc:ImageRegistry"]);
    }

    [Fact]
    public void ImageRewritesAppliedToConfiguration()
    {
        var config = new GlobalConfigFile();
        config.Aspire.Resources["svc"] = new ResourceEntry
        {
            Id = "svc",
            ContainerImage = "myapp",
            ContainerTag = "1.0"
        };
        config.Aspire.Overrides = new OverrideSettings
        {
            ImageRewrites = new Dictionary<string, string>
            {
                ["myapp:1.0"] = "newapp:2.0"
            }
        };

        var path = WriteConfig(config);

        var configuration = new ConfigurationBuilder()
            .AddGlobalResourceConfiguration(path)
            .Build();

        Assert.Equal("newapp", configuration["Aspire:Resources:svc:ContainerImage"]);
        Assert.Equal("2.0", configuration["Aspire:Resources:svc:ContainerTag"]);
    }
}
