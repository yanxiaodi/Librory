using Librory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Librory.Infrastructure.Persistence.Configurations;

internal sealed class BookWorkConfiguration : IEntityTypeConfiguration<BookWork>
{
    public void Configure(EntityTypeBuilder<BookWork> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("book_works");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CanonicalTitle).HasMaxLength(300).IsRequired();
        builder.Property(x => x.CanonicalAuthor).HasMaxLength(300);

        builder.OwnsOne(x => x.Summary, owned =>
        {
            owned.Property(x => x.English).HasColumnName("summary_english").HasMaxLength(4000);
            owned.Property(x => x.Chinese).HasColumnName("summary_chinese").HasMaxLength(4000);
        });

        builder.OwnsOne(x => x.SummaryProvenance, owned =>
        {
            owned.Property(x => x.Source).HasColumnName("summary_source").HasMaxLength(200);
            owned.Property(x => x.SourceId).HasColumnName("summary_source_id").HasMaxLength(200);
            owned.Property(x => x.Confidence).HasColumnName("summary_confidence").HasPrecision(5, 4);
            owned.Property(x => x.CapturedAt).HasColumnName("summary_captured_at");
        });

        builder.OwnsOne(x => x.CanonicalAuthorProvenance, owned =>
        {
            owned.Property(x => x.Source).HasColumnName("canonical_author_source").HasMaxLength(200);
            owned.Property(x => x.SourceId).HasColumnName("canonical_author_source_id").HasMaxLength(200);
            owned.Property(x => x.Confidence).HasColumnName("canonical_author_confidence").HasPrecision(5, 4);
            owned.Property(x => x.CapturedAt).HasColumnName("canonical_author_captured_at");
        });

    }
}
