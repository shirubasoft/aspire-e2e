using ProjectOrImage.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

#if USE_CONTAINER_IMAGES
var apiService = builder.AddContainerImage(
    "apiservice",
    Containers.ProjectOrImage_ApiService.Image,
    Containers.ProjectOrImage_ApiService.Tag,
    Containers.ProjectOrImage_ApiService.Registry)
    .WithHttpEndpoint(targetPort: 8080)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.ProjectOrImage_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService.GetEndpoint("http"))
    .WaitFor(apiService);
#else
var apiService = builder.AddProject<Projects.ProjectOrImage_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.ProjectOrImage_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);
#endif

builder.Build().Run();
