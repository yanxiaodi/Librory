using Librory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Librory.Infrastructure.Persistence.Configurations;

internal sealed class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("wishlist_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Author).HasMaxLength(300);

        builder.HasOne(x => x.Family)
            .WithMany(x => x.WishlistItems)
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.BookWork)
            .WithMany()
            .HasForeignKey(x => x.BookWorkId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.BookEdition)
            .WithMany()
            .HasForeignKey(x => x.BookEditionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
