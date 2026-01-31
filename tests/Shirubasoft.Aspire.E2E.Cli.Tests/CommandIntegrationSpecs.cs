using Shirubasoft.Aspire.E2E.Cli.Commands;
using Shirubasoft.Aspire.E2E.Common;
using Spectre.Console.Cli;
using Xunit;

namespace Shirubasoft.Aspire.E2E.Cli.Tests;

[CollectionDefinition("CommandIntegration", DisableParallelization = true)]
public class CommandIntegrationCollection;

public sealed class CommandIntegrationFixture : IDisposable
{
    public string ConfigPath { get; }

    public CommandIntegrationFixture()
    {
        ConfigPath = Path.Combine(Path.GetTempPath(), $"aspire-e2e-test-{Guid.NewGuid()}.json");
    }

    public void WriteConfig(GlobalConfigFile config)
    {
        config.Save(ConfigPath);
    }

    public GlobalConfigFile ReadConfig()
    {
        return GlobalConfigFile.Load(ConfigPath);
    }

    public CommandApp CreateApp()
    {
        Environment.SetEnvironmentVariable("ASPIRE_E2E_CONFIG_PATH", ConfigPath);

        var app = new CommandApp();
        app.Configure(config =>
        {
            config.SetApplicationName("aspire-e2e");
            config.AddCommand<SearchCommand>("search");
            config.AddCommand<ListCommand>("list");
            config.AddCommand<RemoveCommand>("remove");
            config.AddCommand<UpdateCommand>("update");
            config.AddCommand<BuildCommand>("build");
            config.AddCommand<GetModeCommand>("get-mode");
            config.AddCommand<GetProjectPathCommand>("get-project-path");
            config.AddCommand<GetConfigCommand>("get-config");
            config.AddCommand<ImportCommand>("import");

            config.AddBranch("override", branch =>
            {
                branch.AddCommand<OverrideSetCommand>("set");
                branch.AddCommand<OverrideSetRegistryCommand>("set-registry");
                branch.AddCommand<OverrideRemoveCommand>("remove");
                branch.AddCommand<OverrideRemoveRegistryCommand>("remove-registry");
                branch.AddCommand<OverrideSetImageCommand>("set-image");
                branch.AddCommand<OverrideRemoveImageCommand>("remove-image");
                branch.AddCommand<OverrideListCommand>("list");
                branch.AddCommand<OverrideClearCommand>("clear");
            });
        });

        return app;
    }

    public int Run(CommandApp app, string[] args)
    {
        return app.Run(args, TestContext.Current.CancellationToken);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("ASPIRE_E2E_CONFIG_PATH", null);

        if (File.Exists(ConfigPath))
        {
            File.Delete(ConfigPath);
        }
    }
}

[Collection("CommandIntegration")]
public class ListCommandSpecs : IDisposable
{
    private readonly CommandIntegrationFixture _fixture = new();

    [Fact]
    public void Returns_zero_when_no_resources()
    {
        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["list"]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void Returns_zero_when_resources_exist()
    {
        var config = new GlobalConfigFile();
        config.SetResource("my-svc", new ResourceEntry
        {
            Id = "my-svc",
            Name = "MySvc",
            Mode = "Project",
            ProjectPath = "/some/path.csproj"
        });
        _fixture.WriteConfig(config);

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["list"]);
        Assert.Equal(0, result);
    }

    public void Dispose() => _fixture.Dispose();
}

[Collection("CommandIntegration")]
public class RemoveCommandSpecs : IDisposable
{
    private readonly CommandIntegrationFixture _fixture = new();

    [Fact]
    public void Removes_existing_resource()
    {
        var config = new GlobalConfigFile();
        config.SetResource("to-remove", new ResourceEntry { Id = "to-remove" });
        _fixture.WriteConfig(config);

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["remove", "to-remove"]);

        Assert.Equal(0, result);

        var reloaded = _fixture.ReadConfig();
        Assert.Null(reloaded.GetResource("to-remove"));
    }

    [Fact]
    public void Returns_one_for_nonexistent_resource()
    {
        _fixture.WriteConfig(new GlobalConfigFile());

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["remove", "nonexistent"]);
        Assert.Equal(1, result);
    }

    public void Dispose() => _fixture.Dispose();
}

[Collection("CommandIntegration")]
public class UpdateCommandSpecs : IDisposable
{
    private readonly CommandIntegrationFixture _fixture = new();

    [Fact]
    public void Updates_resource_with_flags()
    {
        var config = new GlobalConfigFile();
        config.SetResource("svc", new ResourceEntry
        {
            Id = "svc",
            Name = "OldName",
            Mode = "Project"
        });
        _fixture.WriteConfig(config);

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["update", "svc", "--name", "NewName", "--mode", "Container"]);

        Assert.Equal(0, result);

        var reloaded = _fixture.ReadConfig();
        var entry = reloaded.GetResource("svc");
        Assert.NotNull(entry);
        Assert.Equal("NewName", entry.Name);
        Assert.Equal("Container", entry.Mode);
    }

    [Fact]
    public void Returns_one_for_nonexistent_resource()
    {
        _fixture.WriteConfig(new GlobalConfigFile());

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["update", "nonexistent", "--name", "X"]);
        Assert.Equal(1, result);
    }

    public void Dispose() => _fixture.Dispose();
}

[Collection("CommandIntegration")]
public class GetModeCommandSpecs : IDisposable
{
    private readonly CommandIntegrationFixture _fixture = new();

    [Fact]
    public void Returns_mode_for_existing_resource()
    {
        var config = new GlobalConfigFile();
        config.SetResource("svc", new ResourceEntry { Id = "svc", Mode = "Container" });
        _fixture.WriteConfig(config);

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["get-mode", "svc"]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void Returns_one_for_nonexistent_resource()
    {
        _fixture.WriteConfig(new GlobalConfigFile());

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["get-mode", "nonexistent"]);
        Assert.Equal(1, result);
    }

    public void Dispose() => _fixture.Dispose();
}

[Collection("CommandIntegration")]
public class GetProjectPathCommandSpecs : IDisposable
{
    private readonly CommandIntegrationFixture _fixture = new();

    [Fact]
    public void Returns_zero_when_path_exists()
    {
        var config = new GlobalConfigFile();
        config.SetResource("svc", new ResourceEntry
        {
            Id = "svc",
            ProjectPath = "/some/path.csproj"
        });
        _fixture.WriteConfig(config);

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["get-project-path", "svc"]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void Returns_one_when_path_is_empty()
    {
        var config = new GlobalConfigFile();
        config.SetResource("svc", new ResourceEntry { Id = "svc" });
        _fixture.WriteConfig(config);

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["get-project-path", "svc"]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void Returns_one_for_nonexistent_resource()
    {
        _fixture.WriteConfig(new GlobalConfigFile());

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["get-project-path", "nonexistent"]);
        Assert.Equal(1, result);
    }

    public void Dispose() => _fixture.Dispose();
}

[Collection("CommandIntegration")]
public class GetConfigCommandSpecs : IDisposable
{
    private readonly CommandIntegrationFixture _fixture = new();

    [Theory]
    [InlineData("Mode", "Container")]
    [InlineData("Name", "TestSvc")]
    [InlineData("ContainerImage", "my-image")]
    [InlineData("ContainerTag", "v1")]
    [InlineData("ProjectPath", "/path/to/project.csproj")]
    [InlineData("BuildImage", "True")]
    public void Returns_zero_for_valid_keys(string key, string _)
    {
        var config = new GlobalConfigFile();
        config.SetResource("svc", new ResourceEntry
        {
            Id = "svc",
            Name = "TestSvc",
            Mode = "Container",
            ContainerImage = "my-image",
            ContainerTag = "v1",
            ProjectPath = "/path/to/project.csproj",
            BuildImage = true,
            BuildImageCommand = "dotnet publish"
        });
        _fixture.WriteConfig(config);

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["get-config", "svc", key]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void Returns_one_for_unknown_key()
    {
        var config = new GlobalConfigFile();
        config.SetResource("svc", new ResourceEntry { Id = "svc" });
        _fixture.WriteConfig(config);

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["get-config", "svc", "UnknownKey"]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void Returns_one_for_nonexistent_resource()
    {
        _fixture.WriteConfig(new GlobalConfigFile());

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["get-config", "nonexistent", "Mode"]);
        Assert.Equal(1, result);
    }

    public void Dispose() => _fixture.Dispose();
}

[Collection("CommandIntegration")]
public class ImportCommandSpecs : IDisposable
{
    private readonly CommandIntegrationFixture _fixture = new();
    private readonly string _importFilePath;

    public ImportCommandSpecs()
    {
        _importFilePath = Path.Combine(Path.GetTempPath(), $"aspire-e2e-import-{Guid.NewGuid()}.json");
    }

    [Fact]
    public void Imports_resources_into_global_config()
    {
        _fixture.WriteConfig(new GlobalConfigFile());

        var source = new GlobalConfigFile();
        source.SetResource("imported-svc", new ResourceEntry
        {
            Id = "imported-svc",
            Name = "ImportedSvc",
            Mode = "Container",
            ProjectPath = "/path/to/project.csproj",
            ContainerImage = "my-image"
        });
        source.Save(_importFilePath);

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["import", _importFilePath]);

        Assert.Equal(0, result);

        var reloaded = _fixture.ReadConfig();
        var entry = reloaded.GetResource("imported-svc");
        Assert.NotNull(entry);
        Assert.Equal("ImportedSvc", entry.Name);
        Assert.Equal("Container", entry.Mode);
    }

    [Fact]
    public void Adds_new_resources_alongside_existing_ones()
    {
        var config = new GlobalConfigFile();
        config.SetResource("existing", new ResourceEntry
        {
            Id = "existing",
            Name = "Existing",
            ProjectPath = "/existing.csproj"
        });
        _fixture.WriteConfig(config);

        var source = new GlobalConfigFile();
        source.SetResource("new-svc", new ResourceEntry
        {
            Id = "new-svc",
            Name = "NewSvc",
            ProjectPath = "/new.csproj"
        });
        source.Save(_importFilePath);

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["import", _importFilePath]);

        Assert.Equal(0, result);

        var reloaded = _fixture.ReadConfig();
        Assert.NotNull(reloaded.GetResource("existing"));
        Assert.NotNull(reloaded.GetResource("new-svc"));
    }

    [Fact]
    public void Merge_updates_only_provided_fields()
    {
        var config = new GlobalConfigFile();
        config.SetResource("svc", new ResourceEntry
        {
            Id = "svc",
            Name = "Original",
            Mode = "Project",
            ProjectPath = "/original.csproj",
            ContainerImage = "old-image"
        });
        _fixture.WriteConfig(config);

        var source = new GlobalConfigFile();
        source.SetResource("svc", new ResourceEntry
        {
            Id = "svc",
            Mode = "Container",
            ContainerImage = "new-image",
            ContainerTag = "v2"
        });
        source.Save(_importFilePath);

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["import", _importFilePath, "--merge"]);

        Assert.Equal(0, result);

        var reloaded = _fixture.ReadConfig();
        var entry = reloaded.GetResource("svc");
        Assert.NotNull(entry);
        Assert.Equal("Original", entry.Name);
        Assert.Equal("Container", entry.Mode);
        Assert.Equal("/original.csproj", entry.ProjectPath);
        Assert.Equal("new-image", entry.ContainerImage);
        Assert.Equal("v2", entry.ContainerTag);
    }

    [Fact]
    public void Merge_preserves_existing_when_source_has_defaults()
    {
        var config = new GlobalConfigFile();
        config.SetResource("svc", new ResourceEntry
        {
            Id = "svc",
            Name = "Existing",
            Mode = "Container",
            ProjectPath = "/path.csproj",
            BuildImage = true,
            BuildImageCommand = "dotnet publish"
        });
        _fixture.WriteConfig(config);

        var source = new GlobalConfigFile();
        source.SetResource("svc", new ResourceEntry
        {
            Id = "svc",
            Name = "Updated"
        });
        source.Save(_importFilePath);

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["import", _importFilePath, "--merge"]);

        Assert.Equal(0, result);

        var reloaded = _fixture.ReadConfig();
        var entry = reloaded.GetResource("svc");
        Assert.NotNull(entry);
        Assert.Equal("Updated", entry.Name);
        Assert.Equal("Container", entry.Mode);
        Assert.True(entry.BuildImage);
        Assert.Equal("dotnet publish", entry.BuildImageCommand);
    }

    [Fact]
    public void Merge_creates_new_resource_when_not_existing()
    {
        _fixture.WriteConfig(new GlobalConfigFile());

        var source = new GlobalConfigFile();
        source.SetResource("new-svc", new ResourceEntry
        {
            Id = "new-svc",
            Name = "NewSvc",
            ProjectPath = "/new.csproj"
        });
        source.Save(_importFilePath);

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["import", _importFilePath, "--merge"]);

        Assert.Equal(0, result);

        var reloaded = _fixture.ReadConfig();
        var entry = reloaded.GetResource("new-svc");
        Assert.NotNull(entry);
        Assert.Equal("NewSvc", entry.Name);
    }

    [Fact]
    public void Returns_one_for_nonexistent_file()
    {
        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["import", "/nonexistent/path.json"]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void Returns_zero_for_empty_source()
    {
        var source = new GlobalConfigFile();
        source.Save(_importFilePath);

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["import", _importFilePath]);
        Assert.Equal(0, result);
    }

    public void Dispose()
    {
        _fixture.Dispose();

        if (File.Exists(_importFilePath))
        {
            File.Delete(_importFilePath);
        }
    }
}
