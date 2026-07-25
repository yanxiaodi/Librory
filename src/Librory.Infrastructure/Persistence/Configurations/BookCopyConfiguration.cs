using Librory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Librory.Infrastructure.Persistence.Configurations;

internal sealed class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("book_copies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DuplicateStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Condition).HasMaxLength(200);
        builder.Property(x => x.PurchaseStore).HasMaxLength(200);
        builder.Property(x => x.PurchasePrice).HasPrecision(18, 2);
        builder.Property(x => x.ShelfLocation).HasMaxLength(200);
        builder.Property(x => x.IntakeNotes).HasMaxLength(4000);

        builder.HasOne(x => x.Family)
            .WithMany(x => x.BookCopies)
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Member)
            .WithMany()
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.BookEdition)
            .WithMany()
            .HasForeignKey(x => x.BookEditionId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
