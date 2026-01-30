# ProjectOrImageReference - Implementation Plan

## Concept

A compile-time MSBuild mechanism that, per resource, either references a local project or builds+uses a container image. Image data, publish commands, and repo paths all live in the AppHost csproj. A comma-separated `ContainerResources` property controls which resources use containers.

## User-Facing API (csproj)

```xml
<PropertyGroup>
  <!-- Comma-separated resource names that should use containers -->
  <!-- Pass via: -p:ContainerResources=ApiService,OtherService -->
  <ContainerResources></ContainerResources>
</PropertyGroup>

<ItemGroup>
  <ProjectOrImageReference Include="ApiService"
      RepoDirectory="api-service"
      ProjectFile="src/ApiService/ApiService.csproj"
      ContainerImage="myregistry.azurecr.io/api-service"
      PublishCommand="./build.sh publish" />
</ItemGroup>
```

- `RepoDirectory`: relative to `$REPOS_BASE_PATH` env var
- `ProjectFile`: path to `.csproj` relative to repo root
- `ContainerImage`: image name (used for docker tag and AddContainer)
- `PublishCommand`: shell command to build the image (run from repo root)

## Mechanism

### MSBuild Targets (`ProjectOrImage.Extensions.targets`)

**Evaluation-time:** All `ProjectOrImageReference` items are added as `ProjectReference` (resolved via `$REPOS_BASE_PATH/%(RepoDirectory)/%(ProjectFile)`).

**Target `_RemoveContainerModeProjectReferences`** (runs before Aspire's `_CreateAspireProjectResources`):
- For items whose Include name IS in `$(ContainerResources)`, removes their `ProjectReference`
- This prevents Aspire from generating `IProjectMetadata` for container-mode resources

**Target `_GenerateProjectOrImageCode`** (runs before `CoreCompile`):

For container-mode items:
1. Gets the git HEAD SHA of `$REPOS_BASE_PATH/%(RepoDirectory)`
2. Checks if `%(ContainerImage):sha-{SHA}` exists locally via `docker image inspect`
3. If not: runs `%(PublishCommand)` from the repo directory, then tags the image
4. If yes: skips the build

**Always generates** three source files:

- `ResourceFlags.g.cs` — per-resource boolean flags
- `ContainerImages.g.cs` — image/tag constants for all resources
- `ProjectStubs.g.cs` — stub `IProjectMetadata` classes for container-mode resources (so `TProject` generic constraint is satisfied)

### Generated Code Example

```csharp
// ResourceFlags.g.cs
namespace ProjectOrImage;
public static class ResourceFlags
{
    public static bool IsContainer(string name) => name switch
    {
        "ApiService" => true,  // or false in project mode
        _ => false,
    };
}

// ContainerImages.g.cs
namespace ProjectOrImage;
public static class ContainerImages
{
    public static class ApiService
    {
        public const string Image = "myregistry.azurecr.io/api-service";
        public const string Tag = "sha-abc1234";  // or "latest" if no SHA resolved
    }
}

// ProjectStubs.g.cs (only when ApiService is in container mode)
namespace Projects;
public class ProjectOrImage_ApiService : IProjectMetadata
{
    public string ProjectPath => "";
    public bool SuppressBuild => true;
}
```

### Runtime Extension Method

```csharp
public static IResourceBuilder<IResource> AddProjectOrImage<TProject>(
    this IDistributedApplicationBuilder builder,
    string name,
    bool isContainer,
    string image,
    string tag)
    where TProject : IProjectMetadata, new()
```

Checks `isContainer`:
- `false` → `builder.AddProject<TProject>(name)`
- `true` → `builder.AddContainer(name, image, tag)`

### AppHost Code (no `#if` blocks)

```csharp
var apiService = builder.AddProjectOrImage<Projects.ProjectOrImage_ApiService>(
    "ApiService",
    ResourceFlags.IsContainer("ApiService"),
    ContainerImages.ApiService.Image,
    ContainerImages.ApiService.Tag)
    .WithHttpEndpoint(targetPort: 8080)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.ProjectOrImage_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService.GetEndpoint("http"))
    .WaitFor(apiService);
```

## Build Commands

```bash
# All as projects (default)
REPOS_BASE_PATH=/path/to/repos dotnet build

# ApiService as container, rest as projects
REPOS_BASE_PATH=/path/to/repos dotnet build -p:ContainerResources=ApiService

# Multiple as containers
REPOS_BASE_PATH=/path/to/repos dotnet build -p:ContainerResources=ApiService,OtherService
```

## Key Design Decisions

1. **Per-resource switching**: Each resource independently switches between project and container mode via `ContainerResources` comma-separated list.
2. **No `#if` blocks**: Runtime `ResourceFlags.IsContainer()` check replaces preprocessor directives.
3. **Stub generation**: Container-mode resources get stub `IProjectMetadata` classes matching Aspire's naming convention (derived from `ProjectFile` filename), so the same `TProject` generic parameter compiles in both modes.
4. **Evaluation-time + target hybrid**: ProjectReferences are added at evaluation time (required for Aspire's source generation), then container-mode ones are removed in a target before Aspire processes them.
5. **Type differences**: Returns `IResourceBuilder<IResource>`. Use endpoint-based references (`.WithReference(apiService.GetEndpoint("http"))`) since `IResource` doesn't satisfy `IResourceWithConnectionString`.
6. **SHA-based tagging**: Container images are tagged with the git SHA of the repo to enable incremental builds.
