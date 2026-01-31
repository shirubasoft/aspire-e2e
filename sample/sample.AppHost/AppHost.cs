using Aspire.Hosting.ApplicationModel;

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
    .WithReference(apiService.As<IResourceWithEndpoints>().GetEndpoint("http"))
    .WaitFor(apiService.InnerBuilder);

builder.Build().Run();
