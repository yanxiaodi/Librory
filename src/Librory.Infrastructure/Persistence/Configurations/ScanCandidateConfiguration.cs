using Librory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Librory.Infrastructure.Persistence.Configurations;

internal sealed class ScanCandidateConfiguration : IEntityTypeConfiguration<ScanCandidate>
{
    public void Configure(EntityTypeBuilder<ScanCandidate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("scan_candidates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DisplayTitle).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Author).HasMaxLength(300);
        builder.Property(x => x.RecommendationScore).HasPrecision(5, 4);
        builder.Property(x => x.DuplicateMessage).HasMaxLength(1000);
        builder.Property(x => x.ConfidenceLabel).HasMaxLength(64).IsRequired();

        builder.HasOne(x => x.ScanSession)
            .WithMany(x => x.Candidates)
            .HasForeignKey(x => x.ScanSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ScanSessionId);
    }
}
