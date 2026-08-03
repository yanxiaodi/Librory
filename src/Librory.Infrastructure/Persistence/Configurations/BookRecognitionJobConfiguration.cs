using Librory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Librory.Infrastructure.Persistence.Configurations;

internal sealed class BookRecognitionJobConfiguration : IEntityTypeConfiguration<BookRecognitionJob>
{
    public void Configure(EntityTypeBuilder<BookRecognitionJob> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("book_recognition_jobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SourcePhotoPath).HasMaxLength(400).IsRequired();
        builder.Property(x => x.Language).HasMaxLength(16);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.ResultJson).HasColumnType("jsonb");
        builder.Property(x => x.FailureMessage).HasMaxLength(2_000);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasOne(x => x.Family)
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.FamilyId, x.CreatedAt });
        builder.HasIndex(x => new { x.FamilyId, x.Status });
    }
}
