using Candoumbe.DataAccess.Abstractions;
using Documents.Objects;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Documents.DataStores;

public class DocumentsStore : DataStore<DocumentsStore>
{
    public DbSet<Document> Documents { get; set; }

    public DocumentsStore(DbContextOptions<DocumentsStore> options, IClock clock) : base(options, clock)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentEntityTypeConfiguration).Assembly);
    }
}