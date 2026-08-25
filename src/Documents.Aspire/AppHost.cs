using Aspire.Hosting;
using Documents.Aspire;
using Microsoft.Extensions.Configuration;
using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
PinnedContainerImage postgresImage = ContainerImages.Postgres;
var postgres = builder.AddPostgres("postgres")
    .WithImage(postgresImage.Image, postgresImage.Tag);
var minio = builder.AddMinioContainer("minio")
    .WithDataVolume();


bool isRunningIntegrationTests = builder.Configuration.GetValue(RunningIntegrationTestsConfigName, false);

if (! isRunningIntegrationTests)
{
    postgres = postgres
        .WithPgAdmin(configureContainer: pgAdmin => pgAdmin.WithImage(ContainerImages.PgAdmin.Image, ContainerImages.PgAdmin.Tag),
                     containerName: "pg-admin");
}
var migrationService = builder.AddProject<Documents_Migrator>("migrations")
    .WithReference(postgres).WaitFor(postgres);

var api = builder.AddProject<Documents_API>("api")
    .WithHttpHealthCheck("/health", endpointName:"http")
    .WithExternalHttpEndpoints()
    // Containerised runs receive no environment name and silently fall back to Production.
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithReference(postgres).WaitFor(postgres)
    .WithReference(minio).WaitFor(minio)
    .WaitForCompletion(migrationService);


await builder.Build().RunAsync();

public partial class Program
{
    public const string RunningIntegrationTestsConfigName = "RunningIntegrationTests";
}