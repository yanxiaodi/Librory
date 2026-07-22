using Librory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Librory.Infrastructure.Persistence;

public sealed class LibroryDbContext : DbContext
{
    public LibroryDbContext(DbContextOptions<LibroryDbContext> options) : base(options)
    {
    }

    public DbSet<Family> Families => Set<Family>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<BookWork> BookWorks => Set<BookWork>();
    public DbSet<BookEdition> BookEditions => Set<BookEdition>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<ScanSession> ScanSessions => Set<ScanSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Family
        var family = modelBuilder.Entity<Family>();
        family.HasKey(f => f.Id);
        family.Property(f => f.Name).HasMaxLength(100).IsRequired();
        family.HasMany(f => f.Members).WithOne(m => m.Family).HasForeignKey(m => m.FamilyId);
        family.HasMany(f => f.BookCopies).WithOne(c => c.Family).HasForeignKey(c => c.FamilyId);
        family.HasMany(f => f.WishlistItems).WithOne(w => w.Family).HasForeignKey(w => w.FamilyId);
        family.HasMany(f => f.ScanSessions).WithOne(s => s.Family).HasForeignKey(s => s.FamilyId);

        // Member
        var member = modelBuilder.Entity<Member>();
        member.HasKey(m => m.Id);
        member.Property(m => m.DisplayName).HasMaxLength(100).IsRequired();
        member.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);
        member.Property(m => m.PreferredLanguage).HasConversion<string>().HasMaxLength(20);
        member.HasOne(m => m.RecommendationProfile)
            .WithOne(rp => rp.Member)
            .HasForeignKey<RecommendationProfile>(rp => rp.MemberId);

        // BookWork
        var bookWork = modelBuilder.Entity<BookWork>();
        bookWork.HasKey(b => b.Id);
        bookWork.Property(b => b.CanonicalTitle).HasMaxLength(255).IsRequired();
        bookWork.Property(b => b.CanonicalAuthor).HasMaxLength(255);
        bookWork.OwnsOne(b => b.Summary, summary =>
        {
            summary.Property(s => s.English).HasMaxLength(4000);
            summary.Property(s => s.Chinese).HasMaxLength(4000);
        });
        bookWork.HasMany(b => b.Editions).WithOne(e => e.BookWork).HasForeignKey(e => e.BookWorkId);

        // BookEdition
        var bookEdition = modelBuilder.Entity<BookEdition>();
        bookEdition.HasKey(e => e.Id);
        bookEdition.Property(e => e.Isbn).HasMaxLength(20);
        bookEdition.Property(e => e.Format).HasMaxLength(50);
        bookEdition.HasMany(e => e.Copies).WithOne(c => c.BookEdition).HasForeignKey(c => c.BookEditionId);

        // BookCopy
        var bookCopy = modelBuilder.Entity<BookCopy>();
        bookCopy.HasKey(c => c.Id);
        bookCopy.Property(c => c.Condition).HasMaxLength(50);
        bookCopy.Property(c => c.PurchaseStore).HasMaxLength(100);
        bookCopy.Property(c => c.PurchasePrice).HasPrecision(18, 2);
        bookCopy.Property(c => c.ShelfLocation).HasMaxLength(100);

        // WishlistItem
        var wishlistItem = modelBuilder.Entity<WishlistItem>();
        wishlistItem.HasKey(w => w.Id);
        wishlistItem.Property(w => w.Title).HasMaxLength(255).IsRequired();
        wishlistItem.Property(w => w.Author).HasMaxLength(255);

        // ScanSession
        var scanSession = modelBuilder.Entity<ScanSession>();
        scanSession.HasKey(s => s.Id);

        // RecommendationProfile
        var recommendationProfile = modelBuilder.Entity<RecommendationProfile>();
        recommendationProfile.HasKey(rp => rp.Id);
        recommendationProfile.Property(rp => rp.MinimumAge);
        recommendationProfile.Property(rp => rp.MaximumAge);
        var stringListComparer = new ValueComparer<List<string>>
        (
            (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
            c => c.ToList()
        );

        recommendationProfile.Property(rp => rp.FavoriteAuthors)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
            .Metadata.SetValueComparer(stringListComparer);
        recommendationProfile.Property(rp => rp.FavoriteGenres)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
            .Metadata.SetValueComparer(stringListComparer);
        recommendationProfile.Property(rp => rp.FavoriteStyles)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
            .Metadata.SetValueComparer(stringListComparer);
    }
}
