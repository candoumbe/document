using Microsoft.Extensions.Configuration;
using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
var postgres = builder.AddPostgres("postgres")
    .WithImage("postgres:17-alpine");


bool isRunningIntegrationTests = builder.Configuration.GetValue(RunningIntegrationTestsConfigName, false);

if (! isRunningIntegrationTests)
{
    postgres = postgres
        .WithPgAdmin(containerName: "pg-admin")
        .WithPgWeb(containerName: "pg-web");
}
var migrationService = builder.AddProject<Documents_Migrator>("migrations")
    .WithReference(postgres)
    .WaitFor(postgres);

var api = builder.AddProject<Documents_API>("api")
    .WithReference(postgres)
    .WaitForCompletion(migrationService);


await builder.Build().RunAsync();

public partial class Program
{
    public const string RunningIntegrationTestsConfigName = "RunningIntegrationTests";
}