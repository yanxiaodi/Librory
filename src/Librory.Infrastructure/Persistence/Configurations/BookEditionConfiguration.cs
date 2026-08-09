using Librory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Librory.Infrastructure.Persistence.Configurations;

internal sealed class BookEditionConfiguration : IEntityTypeConfiguration<BookEdition>
{
    public void Configure(EntityTypeBuilder<BookEdition> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("book_editions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Isbn).HasMaxLength(32);
        builder.Property(x => x.Format).HasMaxLength(64);
        builder.Property(x => x.PublicationYear);

        builder.OwnsOne(x => x.Subtitle, owned =>
        {
            owned.Property(x => x.English).HasColumnName("subtitle_english").HasMaxLength(4000);
            owned.Property(x => x.Chinese).HasColumnName("subtitle_chinese").HasMaxLength(4000);
        });

        builder.OwnsOne(x => x.SubtitleProvenance, owned =>
        {
            owned.Property(x => x.Exists).HasColumnName("subtitle_provenance_exists");
            owned.Property(x => x.Source).HasColumnName("subtitle_source").HasMaxLength(200);
            owned.Property(x => x.SourceId).HasColumnName("subtitle_source_id").HasMaxLength(200);
            owned.Property(x => x.Confidence).HasColumnName("subtitle_confidence").HasPrecision(5, 4);
            owned.Property(x => x.CapturedAt).HasColumnName("subtitle_captured_at");
        });

        builder.OwnsOne(x => x.PublicationYearProvenance, owned =>
        {
            owned.Property(x => x.Exists).HasColumnName("publication_year_provenance_exists");
            owned.Property(x => x.Source).HasColumnName("publication_year_source").HasMaxLength(200);
            owned.Property(x => x.SourceId).HasColumnName("publication_year_source_id").HasMaxLength(200);
            owned.Property(x => x.Confidence).HasColumnName("publication_year_confidence").HasPrecision(5, 4);
            owned.Property(x => x.CapturedAt).HasColumnName("publication_year_captured_at");
        });

        builder.HasOne(x => x.BookWork)
            .WithMany(x => x.Editions)
            .HasForeignKey(x => x.BookWorkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
