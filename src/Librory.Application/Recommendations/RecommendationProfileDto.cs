namespace Librory.Application.Recommendations;

public sealed record RecommendationProfileDto(
    Guid MemberId,
    int? MinimumAge,
    int? MaximumAge,
    IReadOnlyList<string> FavoriteAuthors,
    IReadOnlyList<string> FavoriteGenres,
    IReadOnlyList<string> FavoriteStyles);
