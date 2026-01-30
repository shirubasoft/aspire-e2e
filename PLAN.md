# SharedResourceReference

## Problem summary

User needs a way to run Aspire apphosts between multiple repositories. A specific service within one repository may need to run as a Project, while the same service in another repository may need to run as a Container.

## Expected Usage

### Apphost referencing a Project

```xml
<!-- Apphost referencing the payments-service -->
<ItemGroup>
  <SharedResourceReference Id="payments-service">
    <Name>PaymentsService</Name>
    <Mode>Container</Mode>
    <ProjectPath>~/code/PaymentsService/PaymentsService.csproj</ProjectPath>
    <ContainerImage>localhost/payments-service</ContainerImage>
    <ContainerTag>main</ContainerTag>
    <BuildImageCommand>dotnet publish --os linux --arch x64 /t:PublishContainer</BuildImageCommand>
    <BuildImage>true</BuildImage>
  </SharedResourceReference>
</ItemGroup>
```

All of the settings above, except the Id are optional because they will come from a global source, they are injected to `IConfiguration`. When adding the above `SharedResourceReference` to an Aspire apphost project, the following code is source generated:

```csharp
// PaymentsService is resolved from name coming from global project repository
public static IPaymentsServiceResourceBuilder AddPaymentsService(this IDistributedApplicationBuilder builder)
{
    IResourceBuilder<IResource> resourceBuilder;

    if (builder.Configuration.GetValue<ResourceMode>("Aspire:Resources:payments-service:Mode") == ResourceMode.Container)
    {
        resourceBuilder = builder.AddContainer(
            "payments-service",
            builder.Configuration.GetValue<string>("Aspire:Resources:payments-service:ContainerImage"),
            builder.Configuration.GetValue<string>("Aspire:Resources:payments-service:ContainerTag"));
    }
    else
    {
        resourceBuilder = builder.AddProject(
            "payments-service",
            builder.Configuration.GetValue<string>("Aspire:Resources:payments-service:ProjectPath"));
    }

    return new PaymentsServiceResourceBuilder(resourceBuilder);
}

public sealed class PaymentsServiceResourceBuilder(IResourceBuilder<IResource> resourceBuilder) : ResourceBuilderProxy(resourceBuilder), IPaymentsServiceResourceBuilder
{
    public PaymentsServiceResourceBuilder ConfigureContainer(Action<IResourceBuilder<ContainerResource>> configure)
        => (PaymentsServiceResourceBuilder)base.ConfigureContainer(configure);

    public PaymentsServiceResourceBuilder ConfigureProject(Action<IResourceBuilder<ProjectResource>> configure)
        => (PaymentsServiceResourceBuilder)base.ConfigureProject(configure);

    public PaymentsServiceResourceBuilder Configure<T>(Action<IResourceBuilder<T>> configure) where T : IResource
        => (PaymentsServiceResourceBuilder)base.Configure(configure);
}

public abstract class ResourceBuilderProxy(IResourceBuilder<IResource> resourceBuilder)
{
    public virtual ResourceBuilderProxy ConfigureContainer(Action<IResourceBuilder<ContainerResource>> configure)
    {
        if (resourceBuilder is IResourceBuilder<ContainerResource> containerResourceBuilder)
        {
            configure(containerResourceBuilder);
        }

        return this;
    }

    public virtual ResourceBuilderProxy ConfigureProject(Action<IResourceBuilder<ProjectResource>> configure)
    {
        if (resourceBuilder is IResourceBuilder<ProjectResource> projectResourceBuilder)
        {
            configure(projectResourceBuilder);
        }

        return this;
    }

    public virtual ResourceBuilderProxy Configure<T>(Action<IResourceBuilder<T>> configure) where T : IResource
    {
        if (resourceBuilder.Resource is T typedResource)
        {
            var typedResourceBuilder = (IResourceBuilder<T>)resourceBuilder;

            configure(typedResourceBuilder);
        }

        return this;
    }
}
```

If the resource mode is Container and build image is true, the container image will be built before running the apphost using the specified build image command as part of the apphost build process.
This will be done using MSBuild targets injected by a package reference in the project. Build should use aspire-e2e

### Project

```xml
<!-- payments-service project -->
<ItemGroup>
    <!-- This package ensures the project contains the necessary MSBuild properties to build as a container image -->
    <PackageReference Include="Shirubasoft.Aspire.E2E" Version="x.y.z" />
</ItemGroup>
```

## Global Project Repository

```json
{
  "Aspire": {
    "Resources": {
      "payments-service": {
        "Name": "PaymentsService",
        "Mode": "Container",
        "ContainerImage": "localhost/payments-service",
        "ContainerTag": "main",
        "ProjectPath": "~/code/PaymentsService/PaymentsService.csproj",
        "BuildImage": true,
        "BuildImageCommand": "dotnet publish --os linux --arch x64 /t:PublishContainer"
      }
    }
  }
}
```

This global project repository will be managed by a cli tool called `aspire-e2e`

## CLI Tool Commands

```bash
# Searches for projects referencing Shirubasoft.Aspire.E2E and registers them in the global project repository, interactively asking for missing information
aspire-e2e search [path] --depth [depth=10]

# Lists all registered projects in the global project repository
aspire-e2e list

# Removes a registered project from the global project repository
aspire-e2e remove [id]

# Updates information about a registered project in the global project repository. Interactive if no options are provided
aspire-e2e update [id] --name [name] --mode [mode] --container-image [image] --container-tag [tag] --project-path [path] --build-image [true|false] --build-image-command [command]

# Builds the container image for a registered project in the global project repository. The default tags are the current branch and commit hash. Does not build if the hash is already built, unless --force is provided
aspire-e2e build [id] [--force]
```

## Tech Stack

- .NET 10
- C#
- MSBuild
- Spectre.Console.CLI for CLI tool
- CliWrap for running external commands in the CLI tool
  - Docker/Podman CLI for building container images
  - Git CLI for getting current branch and commit hash
