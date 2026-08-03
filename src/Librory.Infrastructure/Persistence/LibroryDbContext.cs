using Librory.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Librory.Infrastructure.Persistence;

public sealed class LibroryDbContext(DbContextOptions<LibroryDbContext> options) : DbContext(options)
{
    public DbSet<Family> Families => Set<Family>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<BookWork> BookWorks => Set<BookWork>();
    public DbSet<BookEdition> BookEditions => Set<BookEdition>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<ScanSession> ScanSessions => Set<ScanSession>();
    public DbSet<ScanCandidate> ScanCandidates => Set<ScanCandidate>();
    public DbSet<RecommendationProfile> RecommendationProfiles => Set<RecommendationProfile>();
    public DbSet<BookRecognitionJob> BookRecognitionJobs => Set<BookRecognitionJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("librory");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibroryDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
