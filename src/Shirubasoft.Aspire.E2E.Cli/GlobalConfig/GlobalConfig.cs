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
            global.ApplyOverrides();
            return global;
        }

        var local = LoadFile(localPath);

        foreach (var (id, entry) in local.Aspire.Resources)
        {
            global.Aspire.Resources[id] = entry;
        }

        MergeOverrides(global.Aspire, local.Aspire);
        global.ApplyOverrides();

        return global;
    }

    private static void MergeOverrides(AspireConfig target, AspireConfig source)
    {
        if (source.Overrides is null)
        {
            return;
        }

        target.Overrides ??= new OverrideSettings();

        if (source.Overrides.Mode is not null)
        {
            target.Overrides.Mode = source.Overrides.Mode;
        }

        if (source.Overrides.BuildImage is not null)
        {
            target.Overrides.BuildImage = source.Overrides.BuildImage;
        }

        if (source.Overrides.ImageRegistryRewrites is not null)
        {
            target.Overrides.ImageRegistryRewrites ??= [];

            foreach (var (from, to) in source.Overrides.ImageRegistryRewrites)
            {
                target.Overrides.ImageRegistryRewrites[from] = to;
            }
        }

        if (source.Overrides.ImageRewrites is not null)
        {
            target.Overrides.ImageRewrites ??= [];

            foreach (var (from, to) in source.Overrides.ImageRewrites)
            {
                target.Overrides.ImageRewrites[from] = to;
            }
        }
    }

    public void ApplyOverrides()
    {
        if (Aspire.Overrides is null)
        {
            return;
        }

        foreach (var entry in Aspire.Resources.Values)
        {
            if (Aspire.Overrides.Mode is not null)
            {
                entry.Mode = Aspire.Overrides.Mode;
            }

            if (Aspire.Overrides.BuildImage is not null)
            {
                entry.BuildImage = Aspire.Overrides.BuildImage.Value;
            }

            if (Aspire.Overrides.ImageRegistryRewrites is not null && entry.ImageRegistry is not null)
            {
                foreach (var (from, to) in Aspire.Overrides.ImageRegistryRewrites)
                {
                    if (entry.ImageRegistry == from)
                    {
                        entry.ImageRegistry = to;
                    }
                }
            }

            if (Aspire.Overrides.ImageRewrites is not null && entry.ContainerImage is not null)
            {
                var fullImage = entry.ContainerTag is not null
                    ? $"{entry.ContainerImage}:{entry.ContainerTag}"
                    : entry.ContainerImage;

                foreach (var (from, to) in Aspire.Overrides.ImageRewrites)
                {
                    if (fullImage == from)
                    {
                        var colonIndex = to.IndexOf(':');
                        if (colonIndex >= 0)
                        {
                            entry.ContainerImage = to[..colonIndex];
                            entry.ContainerTag = to[(colonIndex + 1)..];
                        }
                        else
                        {
                            entry.ContainerImage = to;
                            entry.ContainerTag = null;
                        }

                        break;
                    }
                }
            }
        }
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

    public static string ResolveSavePath(string? path = null)
    {
        return path ?? ConfigPaths.DefaultConfigPath;
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
    public OverrideSettings? Overrides { get; set; }
    public Dictionary<string, ResourceEntry> Resources { get; set; } = [];
}
