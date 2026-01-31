using Microsoft.Extensions.Configuration;
using Shirubasoft.Aspire.E2E.Common;

namespace Aspire.Hosting;

/// <summary>
/// Extensions to add the global aspire-e2e configuration file to <see cref="IConfigurationBuilder"/>.
/// </summary>
public static class GlobalConfigurationExtensions
{
    public static IConfigurationBuilder AddGlobalResourceConfiguration(
        this IConfigurationBuilder builder,
        string? path = null)
    {
        var config = GlobalConfigFile.Load(path);

        var kvPairs = new Dictionary<string, string?>();

        foreach (var (id, entry) in config.Aspire.Resources)
        {
            var prefix = $"Aspire:Resources:{id}";
            kvPairs[$"{prefix}:Id"] = entry.Id;
            kvPairs[$"{prefix}:Mode"] = entry.Mode;
            kvPairs[$"{prefix}:BuildImage"] = entry.BuildImage.ToString();

            if (entry.Name is not null)
            {
                kvPairs[$"{prefix}:Name"] = entry.Name;
            }

            if (entry.ContainerImage is not null)
            {
                kvPairs[$"{prefix}:ContainerImage"] = entry.ContainerImage;
            }

            if (entry.ContainerTag is not null)
            {
                kvPairs[$"{prefix}:ContainerTag"] = entry.ContainerTag;
            }

            if (entry.ProjectPath is not null)
            {
                kvPairs[$"{prefix}:ProjectPath"] = entry.ProjectPath;
            }

            if (entry.BuildImageCommand is not null)
            {
                kvPairs[$"{prefix}:BuildImageCommand"] = entry.BuildImageCommand;
            }

            if (entry.ImageRegistry is not null)
            {
                kvPairs[$"{prefix}:ImageRegistry"] = entry.ImageRegistry;
            }
        }

        builder.AddInMemoryCollection(kvPairs);

        return builder;
    }
}
