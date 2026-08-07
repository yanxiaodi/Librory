using Librory.Domain.Models;

namespace Librory.Application.Recommendations;

public sealed record RecommendationProfileDto(
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
