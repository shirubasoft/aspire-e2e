namespace Shirubasoft.Aspire.E2E.Common;

public sealed class ResourceEntry
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string Mode { get; set; } = "Project";
    public string? ContainerImage { get; set; }
    public string? ContainerTag { get; set; }
    public string? ProjectPath { get; set; }
    public bool BuildImage { get; set; }
    public string? BuildImageCommand { get; set; }
    public string? ImageRegistry { get; set; }
}
