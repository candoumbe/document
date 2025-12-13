using Documents.DataStores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NodaTime;

namespace Documents.API.Context;

/// <summary>
/// <see cref="IDesignTimeDbContextFactory{TContext}"/> implementation for <see cref="DocumentsStore"/>.
/// </summary>
public class DocumentsDesignTimeDbContextFactory : IDesignTimeDbContextFactory<DocumentsStore>
{
    /// <inheritdoc/>
    public DocumentsStore CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.IntegrationTest.json", optional: true)
            .AddCommandLine(args)
            .Build();

        string provider = configuration.GetValue("provider", "sqlite").ToLowerInvariant();
        DbContextOptionsBuilder<DocumentsStore> builder = new();
        string connectionString = configuration.GetConnectionString("Documents");

        switch (provider)
        {
            case "sqlite":
                builder.UseSqlite(connectionString, b => b.MigrationsAssembly("Documents.DataStores.Sqlite")
                        .UseNodaTime());
                break;
            case "postgres":
                builder.UseNpgsql(connectionString, b => b.MigrationsAssembly("Documents.DataStores.Postgres")
                        .UseNodaTime());
                break;
            default:
                throw new NotSupportedException($"'{provider}' database engine is not currently supported");
        }

        return new DocumentsStore(builder.Options, SystemClock.Instance);
    }
}