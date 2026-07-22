namespace Librory.Domain.Models;

public sealed class Family
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public List<Member> Members { get; } = [];
    public List<BookCopy> BookCopies { get; } = [];
    public List<WishlistItem> WishlistItems { get; } = [];
    public List<RecommendationProfile> RecommendationProfiles { get; } = [];
    public List<ScanSession> ScanSessions { get; } = [];
}
