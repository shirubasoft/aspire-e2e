using System.Text.Json;
using System.Text.Json.Serialization;
using Shirubasoft.Aspire.E2E.Common;

namespace Shirubasoft.Aspire.E2E.Cli.GlobalConfig;

public sealed class GlobalConfigFile
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AspireConfig Aspire { get; set; } = new();

    public static GlobalConfigFile Load(string? path = null)
    {
        var global = LoadFile(path ?? ConfigPaths.DefaultConfigPath);
        var localPath = ConfigPaths.FindLocalConfigFile();

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
        var configPath = path ?? ConfigPaths.DefaultConfigPath;
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
