using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace ProjectOrImage.Extensions;

public static class ProjectOrImageExtensions
{
    /// <summary>
    /// Adds a resource that was resolved at compile time as either a project or container image,
    /// based on the UseContainerImages MSBuild property.
    /// When UseContainerImages is not set, use AddProject with the generated IProjectMetadata type.
    /// When UseContainerImages is set, use AddContainer with image details.
    /// </summary>
    public static IResourceBuilder<ContainerResource> AddContainerImage(
        this IDistributedApplicationBuilder builder,
        string name,
        string image,
        string tag = "latest",
        string? registry = null)
    {
        var container = builder.AddContainer(name, image, tag);

        if (registry is not null)
        {
            container.WithImageRegistry(registry);
        }

        return container;
    }
}
