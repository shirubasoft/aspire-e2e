using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shirubasoft.Aspire.E2E.Cli.GlobalConfig;

public sealed class GlobalConfigFile
{
    public const string LocalConfigFileName = "e2e-resources.json";

    private static readonly string DefaultDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".aspire-e2e");

    private static string DefaultPath =>
        Environment.GetEnvironmentVariable("ASPIRE_E2E_CONFIG_PATH")
        ?? Path.Combine(DefaultDirectory, "resources.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AspireConfig Aspire { get; set; } = new();

    public static GlobalConfigFile Load(string? path = null)
    {
        var global = LoadFile(path ?? DefaultPath);
        var localPath = FindLocalConfigFile();

        if (localPath is null)
        {
            return global;
        }

        var local = LoadFile(localPath);

        foreach (var (id, entry) in local.Aspire.Resources)
        {
            global.Aspire.Resources[id] = entry;
        }

        return global;
    }

    public static string? FindLocalConfigFile(string? startDirectory = null)
    {
        var directory = startDirectory ?? Directory.GetCurrentDirectory();

        while (directory is not null)
        {
            var candidate = Path.Combine(directory, LocalConfigFileName);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            if (Directory.Exists(Path.Combine(directory, ".git")))
            {
                break;
            }

            directory = Path.GetDirectoryName(directory);
        }

        return null;
    }

    public static GlobalConfigFile LoadFile(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return new GlobalConfigFile();
        }

        var json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<GlobalConfigFile>(json, SerializerOptions) ?? new GlobalConfigFile();
    }

    public void Save(string? path = null)
    {
        var configPath = path ?? DefaultPath;
        var directory = Path.GetDirectoryName(configPath)!;

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(this, SerializerOptions);
        File.WriteAllText(configPath, json);
    }

    public ResourceEntry? GetResource(string id)
    {
        Aspire.Resources.TryGetValue(id, out var entry);
        return entry;
    }

    public void SetResource(string id, ResourceEntry entry)
    {
        Aspire.Resources[id] = entry;
    }

    public bool RemoveResource(string id)
    {
        return Aspire.Resources.Remove(id);
    }
}

public sealed class AspireConfig
{
    public Dictionary<string, ResourceEntry> Resources { get; set; } = new();
}
