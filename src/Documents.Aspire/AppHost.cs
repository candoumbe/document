using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var migrationService = builder.AddProject<Documents_Migrator>("migrations")
    .WithReference(postgres)
    .WaitFor(postgres);

var api = builder.AddProject<Documents_API>("api")
    .WithReference(postgres)
    .WaitForCompletion(migrationService);


builder.Build().Run();