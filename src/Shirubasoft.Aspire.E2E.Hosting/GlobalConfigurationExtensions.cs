using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting;

/// <summary>
/// Extensions to add the global aspire-e2e configuration file to <see cref="IConfigurationBuilder"/>.
/// </summary>
public static class GlobalConfigurationExtensions
{
    private static readonly string DefaultConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".aspire-e2e",
        "resources.json");

    public static IConfigurationBuilder AddGlobalResourceConfiguration(
        this IConfigurationBuilder builder,
        string? path = null)
    {
        var configPath = path ?? DefaultConfigPath;

        if (File.Exists(configPath))
        {
            builder.AddJsonFile(configPath, optional: true, reloadOnChange: true);
        }

        return builder;
    }
}
