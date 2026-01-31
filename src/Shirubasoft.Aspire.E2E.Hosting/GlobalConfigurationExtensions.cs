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
        var configPath = path ?? ConfigPaths.DefaultConfigPath;

        if (File.Exists(configPath))
        {
            builder.AddJsonFile(configPath, optional: true, reloadOnChange: true);
        }

        var localConfigPath = ConfigPaths.FindLocalConfigFile();

        if (localConfigPath is not null)
        {
            builder.AddJsonFile(localConfigPath, optional: true, reloadOnChange: true);
        }

        return builder;
    }
}
