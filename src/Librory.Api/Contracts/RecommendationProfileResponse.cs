namespace Librory.Api.Contracts;

public sealed record RecommendationProfileResponse(
    Guid MemberId,
    int? MinimumAge,
    int? MaximumAge,
    IReadOnlyList<string> FavoriteAuthors,
    IReadOnlyList<string> FavoriteGenres,
    IReadOnlyList<string> FavoriteStyles);
