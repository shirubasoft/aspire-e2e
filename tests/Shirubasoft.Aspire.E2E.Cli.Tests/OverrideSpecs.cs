using Shirubasoft.Aspire.E2E.Cli.GlobalConfig;
using Xunit;

namespace Shirubasoft.Aspire.E2E.Cli.Tests;

public class OverrideApplySpecs
{
    [Fact]
    public void Override_Mode_applies_to_all_resources()
    {
        var config = new GlobalConfigFile();
        config.Aspire.Overrides = new OverrideSettings { Mode = "Container" };
        config.SetResource("svc-a", new ResourceEntry { Id = "svc-a", Mode = "Project" });
        config.SetResource("svc-b", new ResourceEntry { Id = "svc-b", Mode = "Project" });

        config.ApplyOverrides();

        Assert.Equal("Container", config.GetResource("svc-a")!.Mode);
        Assert.Equal("Container", config.GetResource("svc-b")!.Mode);
    }

    [Fact]
    public void Override_BuildImage_applies_to_all_resources()
    {
        var config = new GlobalConfigFile();
        config.Aspire.Overrides = new OverrideSettings { BuildImage = true };
        config.SetResource("svc-a", new ResourceEntry { Id = "svc-a", BuildImage = false });
        config.SetResource("svc-b", new ResourceEntry { Id = "svc-b", BuildImage = false });

        config.ApplyOverrides();

        Assert.True(config.GetResource("svc-a")!.BuildImage);
        Assert.True(config.GetResource("svc-b")!.BuildImage);
    }

    [Fact]
    public void Registry_rewrite_transforms_ImageRegistry_values()
    {
        var config = new GlobalConfigFile();
        config.Aspire.Overrides = new OverrideSettings
        {
            ImageRegistryRewrites = new Dictionary<string, string>
            {
                ["docker.io"] = "ghcr.io/myorg"
            }
        };
        config.SetResource("svc", new ResourceEntry { Id = "svc", ImageRegistry = "docker.io" });

        config.ApplyOverrides();

        Assert.Equal("ghcr.io/myorg", config.GetResource("svc")!.ImageRegistry);
    }

    [Fact]
    public void Multiple_registry_rewrite_rules_work()
    {
        var config = new GlobalConfigFile();
        config.Aspire.Overrides = new OverrideSettings
        {
            ImageRegistryRewrites = new Dictionary<string, string>
            {
                ["docker.io"] = "ghcr.io/myorg",
                ["mcr.microsoft.com"] = "myregistry.io"
            }
        };
        config.SetResource("svc-a", new ResourceEntry { Id = "svc-a", ImageRegistry = "docker.io" });
        config.SetResource("svc-b", new ResourceEntry { Id = "svc-b", ImageRegistry = "mcr.microsoft.com" });

        config.ApplyOverrides();

        Assert.Equal("ghcr.io/myorg", config.GetResource("svc-a")!.ImageRegistry);
        Assert.Equal("myregistry.io", config.GetResource("svc-b")!.ImageRegistry);
    }

    [Fact]
    public void Registry_rewrite_skips_non_matching_entries()
    {
        var config = new GlobalConfigFile();
        config.Aspire.Overrides = new OverrideSettings
        {
            ImageRegistryRewrites = new Dictionary<string, string>
            {
                ["docker.io"] = "ghcr.io/myorg"
            }
        };
        config.SetResource("svc", new ResourceEntry { Id = "svc", ImageRegistry = "quay.io" });

        config.ApplyOverrides();

        Assert.Equal("quay.io", config.GetResource("svc")!.ImageRegistry);
    }

    [Fact]
    public void Registry_rewrite_skips_null_ImageRegistry()
    {
        var config = new GlobalConfigFile();
        config.Aspire.Overrides = new OverrideSettings
        {
            ImageRegistryRewrites = new Dictionary<string, string>
            {
                ["docker.io"] = "ghcr.io/myorg"
            }
        };
        config.SetResource("svc", new ResourceEntry { Id = "svc" });

        config.ApplyOverrides();

        Assert.Null(config.GetResource("svc")!.ImageRegistry);
    }

    [Fact]
    public void No_overrides_leaves_resources_unchanged()
    {
        var config = new GlobalConfigFile();
        config.SetResource("svc", new ResourceEntry { Id = "svc", Mode = "Project", BuildImage = false });

        config.ApplyOverrides();

        Assert.Equal("Project", config.GetResource("svc")!.Mode);
        Assert.False(config.GetResource("svc")!.BuildImage);
    }

    [Fact]
    public void Overrides_from_local_merge_on_top_of_global()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"aspire-e2e-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var globalPath = Path.Combine(tempDir, "global.json");
            var localPath = Path.Combine(tempDir, "local.json");

            var global = new GlobalConfigFile();
            global.Aspire.Overrides = new OverrideSettings
            {
                Mode = "Project",
                ImageRegistryRewrites = new Dictionary<string, string>
                {
                    ["docker.io"] = "global-registry.io"
                }
            };
            global.SetResource("svc", new ResourceEntry { Id = "svc", ImageRegistry = "docker.io" });
            global.Save(globalPath);

            var local = new GlobalConfigFile();
            local.Aspire.Overrides = new OverrideSettings
            {
                Mode = "Container",
                ImageRegistryRewrites = new Dictionary<string, string>
                {
                    ["docker.io"] = "local-registry.io"
                }
            };
            local.Save(localPath);

            var loaded = GlobalConfigFile.LoadFile(globalPath);
            var localLoaded = GlobalConfigFile.LoadFile(localPath);

            foreach (var (id, entry) in localLoaded.Aspire.Resources)
            {
                loaded.Aspire.Resources[id] = entry;
            }

            // Simulate the merge that Load() does
            var targetAspire = loaded.Aspire;
            var sourceAspire = localLoaded.Aspire;
            if (sourceAspire.Overrides is not null)
            {
                targetAspire.Overrides ??= new OverrideSettings();
                if (sourceAspire.Overrides.Mode is not null)
                {
                    targetAspire.Overrides.Mode = sourceAspire.Overrides.Mode;
                }
                if (sourceAspire.Overrides.BuildImage is not null)
                {
                    targetAspire.Overrides.BuildImage = sourceAspire.Overrides.BuildImage;
                }
                if (sourceAspire.Overrides.ImageRegistryRewrites is not null)
                {
                    targetAspire.Overrides.ImageRegistryRewrites ??= new Dictionary<string, string>();
                    foreach (var (from, to) in sourceAspire.Overrides.ImageRegistryRewrites)
                    {
                        targetAspire.Overrides.ImageRegistryRewrites[from] = to;
                    }
                }
            }

            loaded.ApplyOverrides();

            Assert.Equal("Container", loaded.GetResource("svc")!.Mode);
            Assert.Equal("local-registry.io", loaded.GetResource("svc")!.ImageRegistry);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Overrides_are_read_time_only_and_not_persisted_to_resources()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"aspire-e2e-test-{Guid.NewGuid()}.json");

        try
        {
            var config = new GlobalConfigFile();
            config.Aspire.Overrides = new OverrideSettings { Mode = "Container" };
            config.SetResource("svc", new ResourceEntry { Id = "svc", Mode = "Project" });
            config.Save(tempPath);

            // Load raw file without applying overrides
            var raw = GlobalConfigFile.LoadFile(tempPath);
            Assert.Equal("Project", raw.GetResource("svc")!.Mode);
            Assert.Equal("Container", raw.Aspire.Overrides!.Mode);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}

[Collection("CommandIntegration")]
public class OverrideCommandSpecs : IDisposable
{
    private readonly CommandIntegrationFixture _fixture = new();

    [Fact]
    public void Set_Mode_override()
    {
        _fixture.WriteConfig(new GlobalConfigFile());

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["override", "set", "Mode", "Container"]);

        Assert.Equal(0, result);

        var config = GlobalConfigFile.LoadFile(_fixture.ConfigPath);
        Assert.Equal("Container", config.Aspire.Overrides?.Mode);
    }

    [Fact]
    public void Set_BuildImage_override()
    {
        _fixture.WriteConfig(new GlobalConfigFile());

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["override", "set", "BuildImage", "true"]);

        Assert.Equal(0, result);

        var config = GlobalConfigFile.LoadFile(_fixture.ConfigPath);
        Assert.True(config.Aspire.Overrides?.BuildImage);
    }

    [Fact]
    public void Set_invalid_key_returns_one()
    {
        _fixture.WriteConfig(new GlobalConfigFile());

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["override", "set", "Invalid", "value"]);

        Assert.Equal(1, result);
    }

    [Fact]
    public void Set_registry_rewrite()
    {
        _fixture.WriteConfig(new GlobalConfigFile());

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["override", "set-registry", "docker.io", "ghcr.io/myorg"]);

        Assert.Equal(0, result);

        var config = GlobalConfigFile.LoadFile(_fixture.ConfigPath);
        Assert.Equal("ghcr.io/myorg", config.Aspire.Overrides?.ImageRegistryRewrites?["docker.io"]);
    }

    [Fact]
    public void Remove_Mode_override()
    {
        var config = new GlobalConfigFile();
        config.Aspire.Overrides = new OverrideSettings { Mode = "Container" };
        _fixture.WriteConfig(config);

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["override", "remove", "Mode"]);

        Assert.Equal(0, result);

        var reloaded = GlobalConfigFile.LoadFile(_fixture.ConfigPath);
        Assert.Null(reloaded.Aspire.Overrides?.Mode);
    }

    [Fact]
    public void Remove_registry_rewrite()
    {
        var config = new GlobalConfigFile();
        config.Aspire.Overrides = new OverrideSettings
        {
            ImageRegistryRewrites = new Dictionary<string, string>
            {
                ["docker.io"] = "ghcr.io/myorg"
            }
        };
        _fixture.WriteConfig(config);

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["override", "remove-registry", "docker.io"]);

        Assert.Equal(0, result);

        var reloaded = GlobalConfigFile.LoadFile(_fixture.ConfigPath);
        Assert.False(reloaded.Aspire.Overrides?.ImageRegistryRewrites?.ContainsKey("docker.io"));
    }

    [Fact]
    public void List_overrides()
    {
        var config = new GlobalConfigFile();
        config.Aspire.Overrides = new OverrideSettings { Mode = "Container", BuildImage = true };
        _fixture.WriteConfig(config);

        var app = _fixture.CreateApp();
        var result = _fixture.Run(app, ["override", "list"]);

        Assert.Equal(0, result);
    }

    public void Dispose() => _fixture.Dispose();
}
