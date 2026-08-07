namespace Librory.Domain.Models;

public sealed class RecommendationProfileChanges
{
    public bool MinimumAgeSpecified { get; init; }
    public int? MinimumAge { get; init; }
    public bool MaximumAgeSpecified { get; init; }
    public int? MaximumAge { get; init; }
    public bool FavoriteAuthorsSpecified { get; init; }
    public IEnumerable<string>? FavoriteAuthors { get; init; }
    public bool ExcludedAuthorsSpecified { get; init; }
    public IEnumerable<string>? ExcludedAuthors { get; init; }
    public bool FavoriteGenresSpecified { get; init; }
    public IEnumerable<string>? FavoriteGenres { get; init; }
    public bool ExcludedGenresSpecified { get; init; }
    public IEnumerable<string>? ExcludedGenres { get; init; }
    public bool FavoriteStylesSpecified { get; init; }
    public IEnumerable<string>? FavoriteStyles { get; init; }
    public bool ExcludedStylesSpecified { get; init; }
    public IEnumerable<string>? ExcludedStyles { get; init; }
    public bool PreferredBookLanguagesSpecified { get; init; }
    public IEnumerable<PreferredLanguage>? PreferredBookLanguages { get; init; }
    public bool PreferenceNotesSpecified { get; init; }
    public string? PreferenceNotes { get; init; }
    public bool ProfileVisibilitySpecified { get; init; }
    public ProfileVisibility ProfileVisibility { get; init; }
    public bool UseInFamilyRecommendationsSpecified { get; init; }
    public bool UseInFamilyRecommendations { get; init; }
}
