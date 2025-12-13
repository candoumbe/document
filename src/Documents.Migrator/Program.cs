using Documents.DataStores;
using Documents.Migrator;
using Microsoft.EntityFrameworkCore;
using NodaTime;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<MigrationWorker>();
builder.Services.AddSingleton<IClock, SystemClock>(_ => SystemClock.Instance);;

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(MigrationWorker.ActivitySourceName));

builder.AddNpgsqlDbContext<DocumentsStore>("postgres",
    configureDbContextOptions: optionsBuilder => optionsBuilder.UseNpgsql(o => o.UseNodaTime()
        .MigrationsAssembly("Documents.DataStores.Postgres")));

IHost host = builder.Build();

await host.RunAsync();