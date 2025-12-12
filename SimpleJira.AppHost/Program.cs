using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("simplejira");

var apiService = builder
    .AddProject<Projects.SimpleJira_ApiService>("apiservice")
    .WithReference(database)
    .WithExternalHttpEndpoints(); // expose API endpoint in dashboard

builder.AddProject<Projects.SimpleJira_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
