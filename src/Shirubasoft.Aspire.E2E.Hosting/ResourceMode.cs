namespace Aspire.Hosting;

/// <summary>
/// Determines whether a shared resource runs as a project reference or a container image.
/// </summary>
public enum ResourceMode
{
    Project,
    Container
}
