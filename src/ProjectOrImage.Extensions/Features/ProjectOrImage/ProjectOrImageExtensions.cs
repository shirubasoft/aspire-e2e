using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace ProjectOrImage.Extensions;

public static class ProjectOrImageExtensions
{
    /// <summary>
    /// Adds a resource that is resolved at compile time as either a project reference or a container image,
    /// based on the per-resource <c>ContainerResources</c> MSBuild property.
    /// </summary>
    public static IResourceBuilder<IResourceWithEndpoints> AddProjectOrImage<TProject>(
        this IDistributedApplicationBuilder builder,
        string name,
        bool isContainer,
        string image,
        string tag)
        where TProject : IProjectMetadata, new()
    {
        if (isContainer)
        {
            return new ResourceBuilderWrapper<IResourceWithEndpoints>(builder.AddContainer(name, image, tag));
        }

        return new ResourceBuilderWrapper<IResourceWithEndpoints>(builder.AddProject<TProject>(name));
    }

    private sealed class ResourceBuilderWrapper<T>(IResourceBuilder<IResource> inner) : IResourceBuilder<T>
        where T : IResource
    {
        public T Resource => (T)inner.Resource;
        public IDistributedApplicationBuilder ApplicationBuilder => inner.ApplicationBuilder;

        public IResourceBuilder<T> WithAnnotation<TAnnotation>(TAnnotation annotation, ResourceAnnotationMutationBehavior behavior = ResourceAnnotationMutationBehavior.Append)
            where TAnnotation : IResourceAnnotation
        {
            inner.WithAnnotation(annotation, behavior);
            return this;
        }
    }
}
