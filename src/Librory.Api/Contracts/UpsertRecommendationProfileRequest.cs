namespace Librory.Api.Contracts;

public sealed record UpsertRecommendationProfileRequest(
    int? MinimumAge = null,
    int? MaximumAge = null,
    IReadOnlyList<string>? FavoriteAuthors = null,
    IReadOnlyList<string>? FavoriteGenres = null,
    IReadOnlyList<string>? FavoriteStyles = null);
