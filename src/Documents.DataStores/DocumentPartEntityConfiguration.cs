using Documents.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Documents.DataStores;

/// <summary>
/// Entity configuration for <see cref="DocumentPart"/>
/// </summary>
public class DocumentPartEntityConfiguration : IEntityTypeConfiguration<DocumentPart>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DocumentPart> builder)
    {
        builder.HasKey(x => new { x.DocumentId, x.Position });

        builder.Property(x => x.Content)
            .IsRequired();
    }
}