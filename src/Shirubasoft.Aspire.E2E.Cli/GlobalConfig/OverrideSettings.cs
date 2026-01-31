namespace Shirubasoft.Aspire.E2E.Cli.GlobalConfig;

public sealed class OverrideSettings
{
    public string? Mode { get; set; }
    public bool? BuildImage { get; set; }
    public Dictionary<string, string>? ImageRegistryRewrites { get; set; }
    public Dictionary<string, string>? ImageRewrites { get; set; }
}
