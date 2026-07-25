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

        builder.HasIndex(x => new { x.FamilyId, x.CreatedAt });
        builder.HasIndex(x => x.ExpiresAt);
    }
}
