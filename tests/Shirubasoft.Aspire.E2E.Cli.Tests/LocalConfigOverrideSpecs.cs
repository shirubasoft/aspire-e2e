using Shirubasoft.Aspire.E2E.Cli.GlobalConfig;
using Xunit;

namespace Shirubasoft.Aspire.E2E.Cli.Tests;

public class LocalConfigOverrideSpecs : IDisposable
{
    private readonly string _tempDir;

    public LocalConfigOverrideSpecs()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"aspire-e2e-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void FindLocalConfigFile_returns_null_when_no_file_exists()
    {
        // Create a .git dir so the walk stops here
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));

        var result = GlobalConfigFile.FindLocalConfigFile(_tempDir);

        Assert.Null(result);
    }

    [Fact]
    public void FindLocalConfigFile_finds_file_in_start_directory()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));
        var configPath = Path.Combine(_tempDir, GlobalConfigFile.LocalConfigFileName);
        File.WriteAllText(configPath, "{}");

        var result = GlobalConfigFile.FindLocalConfigFile(_tempDir);

        Assert.Equal(configPath, result);
    }

    [Fact]
    public void FindLocalConfigFile_walks_up_to_git_root()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));
        var configPath = Path.Combine(_tempDir, GlobalConfigFile.LocalConfigFileName);
        File.WriteAllText(configPath, "{}");

        var subDir = Path.Combine(_tempDir, "src", "project");
        Directory.CreateDirectory(subDir);

        var result = GlobalConfigFile.FindLocalConfigFile(subDir);

        Assert.Equal(configPath, result);
    }

    [Fact]
    public void FindLocalConfigFile_stops_at_git_root()
    {
        // Parent has a .git dir and the file, but child also has .git
        var parent = _tempDir;
        Directory.CreateDirectory(Path.Combine(parent, ".git"));
        File.WriteAllText(Path.Combine(parent, GlobalConfigFile.LocalConfigFileName), "{}");

        var child = Path.Combine(parent, "child");
        Directory.CreateDirectory(child);
        Directory.CreateDirectory(Path.Combine(child, ".git"));

        // Starting from child, should stop at child's .git and not find the file
        var result = GlobalConfigFile.FindLocalConfigFile(child);

        Assert.Null(result);
    }

    [Fact]
    public void Local_config_overrides_global_resources()
    {
        var globalPath = Path.Combine(_tempDir, "global.json");
        var localPath = Path.Combine(_tempDir, "local.json");

        var global = new GlobalConfigFile();
        global.SetResource("svc-a", new ResourceEntry { Id = "svc-a", Mode = "Project" });
        global.SetResource("svc-b", new ResourceEntry { Id = "svc-b", Mode = "Project" });
        global.Save(globalPath);

        var local = new GlobalConfigFile();
        local.SetResource("svc-a", new ResourceEntry { Id = "svc-a", Mode = "Container" });
        local.Save(localPath);

        // Simulate merge: load global then overlay local
        var loaded = GlobalConfigFile.LoadFile(globalPath);
        var localLoaded = GlobalConfigFile.LoadFile(localPath);

        foreach (var (id, entry) in localLoaded.Aspire.Resources)
        {
            loaded.Aspire.Resources[id] = entry;
        }

        Assert.Equal("Container", loaded.GetResource("svc-a")!.Mode);
        Assert.Equal("Project", loaded.GetResource("svc-b")!.Mode);
    }

    [Fact]
    public void Local_config_adds_new_resources()
    {
        var globalPath = Path.Combine(_tempDir, "global.json");
        var localPath = Path.Combine(_tempDir, "local.json");

        var global = new GlobalConfigFile();
        global.SetResource("svc-a", new ResourceEntry { Id = "svc-a", Mode = "Project" });
        global.Save(globalPath);

        var local = new GlobalConfigFile();
        local.SetResource("svc-local", new ResourceEntry { Id = "svc-local", Mode = "Container" });
        local.Save(localPath);

        var loaded = GlobalConfigFile.LoadFile(globalPath);
        var localLoaded = GlobalConfigFile.LoadFile(localPath);

        foreach (var (id, entry) in localLoaded.Aspire.Resources)
        {
            loaded.Aspire.Resources[id] = entry;
        }

        Assert.NotNull(loaded.GetResource("svc-a"));
        Assert.NotNull(loaded.GetResource("svc-local"));
        Assert.Equal(2, loaded.Aspire.Resources.Count);
    }
}
