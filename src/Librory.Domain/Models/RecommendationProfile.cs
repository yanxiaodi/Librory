namespace Librory.Domain.Models;

public sealed class RecommendationProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid MemberId { get; set; }
    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }
    public List<string> FavoriteAuthors { get; } = [];
    public List<string> FavoriteGenres { get; } = [];
    public List<string> FavoriteStyles { get; } = [];
    public Member Member { get; set; } = null!;
}
