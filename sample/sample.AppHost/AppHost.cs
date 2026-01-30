using Shirubasoft.Aspire.E2E.Hosting;
using Shirubasoft.Aspire.E2E.Hosting.Generated;

var builder = DistributedApplication.CreateBuilder(args);

builder.Configuration.AddGlobalResourceConfiguration();

var apiService = builder.AddSampleApiService()
    .ConfigureProject(project => project
        .WithHttpHealthCheck("/health"))
    .ConfigureContainer(container => container
        .WithHttpEndpoint(targetPort: 8080)
        .WithHttpHealthCheck("/health"));

builder.AddProject<Projects.sample_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(apiService.InnerBuilder);

builder.Build().Run();
