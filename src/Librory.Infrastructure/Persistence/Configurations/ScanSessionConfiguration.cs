using Librory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Librory.Infrastructure.Persistence.Configurations;

internal sealed class ScanSessionConfiguration : IEntityTypeConfiguration<ScanSession>
{
    public void Configure(EntityTypeBuilder<ScanSession> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("scan_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ShelfPhotoPath).HasMaxLength(400).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.TargetProfileAvailable).IsRequired();
        builder.Property(x => x.TargetProfileUsed).IsRequired();
        builder.Property(x => x.InferredLanguage).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.HasMixedLanguages).IsRequired();

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(x => x.TargetMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.FamilyId, x.CreatedAt });
        builder.HasIndex(x => new { x.FamilyId, x.TargetMemberId });
        builder.HasIndex(x => x.ExpiresAt);
    }
}
