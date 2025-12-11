using Candoumbe.Types.Numerics;
using Documents.Ids;
using Documents.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Documents.DataStores;

/// <summary>
/// Entity configuration for <see cref="Document"/>
/// </summary>
public class DocumentEntityTypeConfiguration : IEntityTypeConfiguration<Document>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.Property(x => x.Id)
            .HasConversion<DocumentId.EfCoreValueConverter>();
        builder.HasMany(x => x.Parts)
            .WithOne()
            .HasForeignKey(part => part.DocumentId)
            .HasPrincipalKey(doc => doc.Id);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasDefaultValue(Status.Ongoing);

        builder.Property(x => x.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.MimeType)
            .IsRequired()
            .HasMaxLength(255)
            .HasDefaultValue(Document.DefaultMimeType);

        builder.Property(x => x.Size)
            .HasConversion(valueObject => valueObject.Value, value => NonNegativeLong.From(value));

    }
}