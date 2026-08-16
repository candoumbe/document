using Documents.Aspire;
using Microsoft.Extensions.Configuration;
using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
var postgres = builder.AddPostgres("postgres")
    .WithImage("postgres:17-alpine");


bool isRunningIntegrationTests = builder.Configuration.GetValue(RunningIntegrationTestsConfigName, false);

if (! isRunningIntegrationTests)
{
    PinnedContainerImage postgresImage = ContainerImages.Postgres;
    postgres = postgres
        .WithPgAdmin(configureContainer: pgAdmin => pgAdmin.WithImage(postgresImage.Image, postgresImage.Tag),
                     containerName: "pg-admin")
        .WithPgWeb(configureContainer: pgWeb => pgWeb.WithImage(ContainerImages.PgAdmin.Image, ContainerImages.PgAdmin.Tag),
                   containerName: "pg-web");
}
var migrationService = builder.AddProject<Documents_Migrator>("migrations")
    .WithReference(postgres).WaitFor(postgres);

var api = builder.AddProject<Documents_API>("api")
    .WithHttpHealthCheck("/health", endpointName:"http")
    .WithExternalHttpEndpoints()
    // Containerised runs receive no environment name and silently fall back to Production.
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithReference(postgres).WaitFor(postgres)
    .WaitForCompletion(migrationService);


await builder.Build().RunAsync();

public partial class Program
{
    public const string RunningIntegrationTestsConfigName = "RunningIntegrationTests";
}