using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public sealed record RecommendationProfileResponse(
    Guid MemberId,
    int? MinimumAge,
    int? MaximumAge,
    IReadOnlyList<string> FavoriteAuthors,
    IReadOnlyList<string> ExcludedAuthors,
    IReadOnlyList<string> FavoriteGenres,
    IReadOnlyList<string> ExcludedGenres,
    IReadOnlyList<string> FavoriteStyles,
    IReadOnlyList<string> ExcludedStyles,
    IReadOnlyList<PreferredLanguage> PreferredBookLanguages,
    string? PreferenceNotes,
    ProfileVisibility ProfileVisibility,
    bool UseInFamilyRecommendations);
