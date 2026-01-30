using Shirubasoft.Aspire.E2E.Cli.GlobalConfig;
using Xunit;

namespace Shirubasoft.Aspire.E2E.Cli.Tests;

public class GlobalConfigSerializationSpecs
{
    [Fact]
    public void Round_trips_config_through_save_and_load()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"aspire-e2e-test-{Guid.NewGuid()}.json");

        try
        {
            var config = new GlobalConfigFile();
            config.SetResource("test-service", new ResourceEntry
            {
                Id = "test-service",
                Name = "TestService",
                Mode = "Container",
                ContainerImage = "test-service",
                ContainerTag = "main",
                ProjectPath = "/path/to/project.csproj",
                BuildImage = true,
                BuildImageCommand = "dotnet publish /t:PublishContainer"
            });

            config.Save(tempPath);

            var loaded = GlobalConfigFile.Load(tempPath);

            var entry = loaded.GetResource("test-service");
            Assert.NotNull(entry);
            Assert.Equal("TestService", entry.Name);
            Assert.Equal("Container", entry.Mode);
            Assert.Equal("test-service", entry.ContainerImage);
            Assert.Equal("main", entry.ContainerTag);
            Assert.True(entry.BuildImage);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void Returns_empty_config_when_file_does_not_exist()
    {
        var config = GlobalConfigFile.Load("/nonexistent/path.json");
        Assert.Empty(config.Aspire.Resources);
    }

    [Fact]
    public void Removes_resource_from_config()
    {
        var config = new GlobalConfigFile();
        config.SetResource("to-remove", new ResourceEntry { Id = "to-remove" });

        Assert.True(config.RemoveResource("to-remove"));
        Assert.Null(config.GetResource("to-remove"));
    }

    [Fact]
    public void Remove_returns_false_for_nonexistent_resource()
    {
        var config = new GlobalConfigFile();
        Assert.False(config.RemoveResource("nonexistent"));
    }

    [Fact]
    public void Round_trips_SkipImageBuild_and_ImageRegistry()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"aspire-e2e-test-{Guid.NewGuid()}.json");

        try
        {
            var config = new GlobalConfigFile();
            config.SetResource("registry-svc", new ResourceEntry
            {
                Id = "registry-svc",
                Mode = "Container",
                SkipImageBuild = true,
                ImageRegistry = "ghcr.io/myorg"
            });

            config.Save(tempPath);

            var loaded = GlobalConfigFile.Load(tempPath);
            var entry = loaded.GetResource("registry-svc");

            Assert.NotNull(entry);
            Assert.True(entry.SkipImageBuild);
            Assert.Equal("ghcr.io/myorg", entry.ImageRegistry);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void SkipImageBuild_defaults_to_false()
    {
        var entry = new ResourceEntry { Id = "test" };
        Assert.False(entry.SkipImageBuild);
    }

    [Fact]
    public void ImageRegistry_defaults_to_null()
    {
        var entry = new ResourceEntry { Id = "test" };
        Assert.Null(entry.ImageRegistry);
    }
}
