using ProjectOrImage;
using ProjectOrImage.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

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

builder.Build().Run();
