using System.Text.Json;
using System.Text.Json.Serialization;
using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public sealed class UpsertRecommendationProfileRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement MinimumAge { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement MaximumAge { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement FavoriteAuthors { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement ExcludedAuthors { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement FavoriteGenres { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement ExcludedGenres { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement FavoriteStyles { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement ExcludedStyles { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement PreferredBookLanguages { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement PreferenceNotes { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement ProfileVisibility { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement UseInFamilyRecommendations { get; init; }

    public UpsertRecommendationProfileRequest()
    {
    }

    public UpsertRecommendationProfileRequest(
        int? minimumAge = null,
        int? maximumAge = null,
        IReadOnlyList<string>? favoriteAuthors = null,
        IReadOnlyList<string>? favoriteGenres = null,
        IReadOnlyList<string>? favoriteStyles = null,
        IReadOnlyList<string>? excludedAuthors = null,
        IReadOnlyList<string>? excludedGenres = null,
        IReadOnlyList<string>? excludedStyles = null,
        IReadOnlyList<PreferredLanguage>? preferredBookLanguages = null,
        string? preferenceNotes = null,
        ProfileVisibility? profileVisibility = null,
        bool? useInFamilyRecommendations = null)
    {
        MinimumAge = ToElement(minimumAge);
        MaximumAge = ToElement(maximumAge);
        FavoriteAuthors = ToElement(favoriteAuthors);
        FavoriteGenres = ToElement(favoriteGenres);
        FavoriteStyles = ToElement(favoriteStyles);
        ExcludedAuthors = ToElement(excludedAuthors);
        ExcludedGenres = ToElement(excludedGenres);
        ExcludedStyles = ToElement(excludedStyles);
        PreferredBookLanguages = ToElement(preferredBookLanguages);
        PreferenceNotes = ToElement(preferenceNotes);
        ProfileVisibility = ToElement(profileVisibility);
        UseInFamilyRecommendations = ToElement(useInFamilyRecommendations);
    }

    public RecommendationProfileChanges ToChanges() => new()
    {
        MinimumAgeSpecified = IsSpecified(MinimumAge),
        MinimumAge = ReadNullableStruct<int>(MinimumAge),
        MaximumAgeSpecified = IsSpecified(MaximumAge),
        MaximumAge = ReadNullableStruct<int>(MaximumAge),
        FavoriteAuthorsSpecified = IsSpecified(FavoriteAuthors),
        FavoriteAuthors = ReadList<string>(FavoriteAuthors),
        ExcludedAuthorsSpecified = IsSpecified(ExcludedAuthors),
        ExcludedAuthors = ReadList<string>(ExcludedAuthors),
        FavoriteGenresSpecified = IsSpecified(FavoriteGenres),
        FavoriteGenres = ReadList<string>(FavoriteGenres),
        ExcludedGenresSpecified = IsSpecified(ExcludedGenres),
        ExcludedGenres = ReadList<string>(ExcludedGenres),
        FavoriteStylesSpecified = IsSpecified(FavoriteStyles),
        FavoriteStyles = ReadList<string>(FavoriteStyles),
        ExcludedStylesSpecified = IsSpecified(ExcludedStyles),
        ExcludedStyles = ReadList<string>(ExcludedStyles),
        PreferredBookLanguagesSpecified = IsSpecified(PreferredBookLanguages),
        PreferredBookLanguages = ReadList<PreferredLanguage>(PreferredBookLanguages),
        PreferenceNotesSpecified = IsSpecified(PreferenceNotes),
        PreferenceNotes = ReadNullableReference<string>(PreferenceNotes),
        ProfileVisibilitySpecified = IsNonNullSpecified(ProfileVisibility),
        ProfileVisibility = ReadValue(ProfileVisibility, default(ProfileVisibility)),
        UseInFamilyRecommendationsSpecified = IsNonNullSpecified(UseInFamilyRecommendations),
        UseInFamilyRecommendations = ReadValue(UseInFamilyRecommendations, true),
    };

    private static bool IsSpecified(JsonElement element) => element.ValueKind != JsonValueKind.Undefined;

    private static bool IsNonNullSpecified(JsonElement element) =>
        element.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null);

    private static JsonElement ToElement<T>(T? value) where T : struct =>
        value.HasValue ? JsonSerializer.SerializeToElement(value.Value) : default;

    private static JsonElement ToElement<T>(IReadOnlyList<T>? value) =>
        value is null ? default : JsonSerializer.SerializeToElement(value);

    private static JsonElement ToElement(string? value) =>
        value is null ? default : JsonSerializer.SerializeToElement(value);

    private static T? ReadNullableStruct<T>(JsonElement element) where T : struct =>
        element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => element.Deserialize<T>()!,
        };

    private static T? ReadNullableReference<T>(JsonElement element) where T : class =>
        element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => element.Deserialize<T>()!,
        };

    private static IReadOnlyList<T>? ReadList<T>(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element.Deserialize<List<T>>() ?? [],
        };

    private static T ReadValue<T>(JsonElement element, T fallback) =>
        element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            ? fallback
            : element.Deserialize<T>()!;
}
