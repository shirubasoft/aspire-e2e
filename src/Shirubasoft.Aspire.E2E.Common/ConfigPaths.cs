namespace Shirubasoft.Aspire.E2E.Common;

public static class ConfigPaths
{
    public const string LocalConfigFileName = "e2e-resources.json";

    private static readonly string DefaultDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".aspire-e2e");

    public static string DefaultConfigPath =>
        Environment.GetEnvironmentVariable("ASPIRE_E2E_CONFIG_PATH")
        ?? Path.Combine(DefaultDirectory, "resources.json");

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
}
