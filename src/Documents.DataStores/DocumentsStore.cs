using Candoumbe.Types.Numerics;
using Documents.Ids;
using Documents.Objects;

namespace Documents.DataStores;

using Candoumbe.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;
using NodaTime;

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

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasMany(x => x.Parts)
                .WithOne()
                .HasForeignKey(part => part.DocumentId)
                .HasPrincipalKey(doc => doc.Id);

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasDefaultValue(Status.Ongoing);

            entity.Property(x => x.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(x => x.MimeType)
                .IsRequired()
                .HasMaxLength(255)
                .HasDefaultValue(Document.DefaultMimeType);

            entity.Property(x => x.Size)
                .HasConversion(valueObject => valueObject.Value, value => NonNegativeLong.From(value));
        });

        modelBuilder.Entity<Document>().Property(x => x.Id).HasConversion<DocumentId.EfCoreValueConverter>();


        modelBuilder.Entity<DocumentPart>(file =>
        {
            file.HasKey(x => new { x.DocumentId, x.Position });

            file.Property(f => f.Content)
                .IsRequired();
        });
    }
}