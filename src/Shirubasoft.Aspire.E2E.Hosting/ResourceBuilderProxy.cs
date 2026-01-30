using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Shirubasoft.Aspire.E2E.Hosting;

/// <summary>
/// Wraps an <see cref="IResourceBuilder{IResource}"/> and provides mode-aware configuration methods.
/// </summary>
public abstract class ResourceBuilderProxy(IResourceBuilder<IResource> resourceBuilder)
{
    public IResourceBuilder<IResource> InnerBuilder { get; } = resourceBuilder;

    public virtual ResourceBuilderProxy ConfigureContainer(Action<IResourceBuilder<ContainerResource>> configure)
    {
        if (InnerBuilder is IResourceBuilder<ContainerResource> containerResourceBuilder)
        {
            configure(containerResourceBuilder);
        }

        return this;
    }

    public virtual ResourceBuilderProxy ConfigureProject(Action<IResourceBuilder<ProjectResource>> configure)
    {
        if (InnerBuilder is IResourceBuilder<ProjectResource> projectResourceBuilder)
        {
            configure(projectResourceBuilder);
        }

        return this;
    }

    public virtual ResourceBuilderProxy Configure<T>(Action<IResourceBuilder<T>> configure) where T : IResource
    {
        if (InnerBuilder.Resource is T)
        {
            var typedResourceBuilder = (IResourceBuilder<T>)InnerBuilder;
            configure(typedResourceBuilder);
        }

        return this;
    }

    /// <summary>
    /// Returns the inner builder as <see cref="IResourceBuilder{T}"/> if the resource is of type <typeparamref name="T"/>;
    /// otherwise throws <see cref="InvalidOperationException"/>.
    /// </summary>
    public IResourceBuilder<T> As<T>() where T : IResource
    {
        if (InnerBuilder.Resource is T)
        {
            return (IResourceBuilder<T>)InnerBuilder;
        }

        throw new InvalidOperationException(
            $"Resource '{InnerBuilder.Resource.Name}' is of type '{InnerBuilder.Resource.GetType().Name}', not '{typeof(T).Name}'.");
    }
}
