using Librory.Api.Contracts;

namespace Librory.Api.Validation;

internal static class MetadataCandidateValidation
{
    public const int MaxMatchCount = 20;
    public const int MaxSourceLength = 200;
    public const int MaxSourceIdLength = 200;
    public const int MaxTitleLength = 300;
    public const int MaxSubtitleLength = 4000;
    public const int MaxAuthorCount = 20;
    public const int MaxAuthorLength = 300;
    public const int MaxPublisherLength = 300;
    public const int MaxPublishedDateLength = 100;
    public const int MaxLanguageLength = 32;
    public const int MaxDescriptionLength = 4000;
    public const int MaxIsbnLength = 32;
    public const int MaxUrlLength = 2000;

    public static Dictionary<string, string[]> Validate(
        BookMetadataImportCandidateRequest candidate,
        string keyPrefix)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyPrefix);

        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        AddRequired(errors, $"{keyPrefix}.source", candidate.Source, "Metadata source is required.");
        AddRequired(errors, $"{keyPrefix}.sourceId", candidate.SourceId, "Metadata source id is required.");
        AddRequired(errors, $"{keyPrefix}.title", candidate.Title, "Metadata title is required.");
        AddMaxLength(errors, $"{keyPrefix}.source", candidate.Source, MaxSourceLength);
        AddMaxLength(errors, $"{keyPrefix}.sourceId", candidate.SourceId, MaxSourceIdLength);
        AddMaxLength(errors, $"{keyPrefix}.title", candidate.Title, MaxTitleLength);
        AddMaxLength(errors, $"{keyPrefix}.subtitle", candidate.Subtitle, MaxSubtitleLength);
        AddMaxLength(errors, $"{keyPrefix}.publisher", candidate.Publisher, MaxPublisherLength);
        AddMaxLength(errors, $"{keyPrefix}.publishedDate", candidate.PublishedDate, MaxPublishedDateLength);
        AddMaxLength(errors, $"{keyPrefix}.language", candidate.Language, MaxLanguageLength);
        AddMaxLength(errors, $"{keyPrefix}.description", candidate.Description, MaxDescriptionLength);
        AddMaxLength(errors, $"{keyPrefix}.isbn10", candidate.Isbn10, MaxIsbnLength);
        AddMaxLength(errors, $"{keyPrefix}.isbn13", candidate.Isbn13, MaxIsbnLength);
        AddMaxLength(errors, $"{keyPrefix}.thumbnailUrl", candidate.ThumbnailUrl, MaxUrlLength);
        AddMaxLength(errors, $"{keyPrefix}.infoUrl", candidate.InfoUrl, MaxUrlLength);

        if (candidate.Authors is { Count: > MaxAuthorCount })
        {
            Add(errors, $"{keyPrefix}.authors", $"Metadata authors must contain {MaxAuthorCount} entries or fewer.");
        }

        if (candidate.Authors is not null)
        {
            for (var index = 0; index < candidate.Authors.Count; index++)
            {
                var author = candidate.Authors[index];
                if (string.IsNullOrWhiteSpace(author))
                {
                    Add(errors, $"{keyPrefix}.authors[{index}]", "Author entries must not be blank.");
                }

                AddMaxLength(errors, $"{keyPrefix}.authors[{index}]", author, MaxAuthorLength);
            }
        }

        return errors.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToArray(),
            StringComparer.Ordinal);
    }

    public static void Merge(
        IDictionary<string, string[]> destination,
        IReadOnlyDictionary<string, string[]> source)
    {
        foreach (var pair in source)
        {
            destination[pair.Key] = pair.Value;
        }
    }

    private static void AddRequired(
        IDictionary<string, List<string>> errors,
        string key,
        string? value,
        string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Add(errors, key, message);
        }
    }

    private static void AddMaxLength(
        IDictionary<string, List<string>> errors,
        string key,
        string? value,
        int maxLength)
    {
        if (value is not null && value.Trim().Length > maxLength)
        {
            Add(errors, key, $"Value must be {maxLength} characters or fewer.");
        }
    }

    private static void Add(
        IDictionary<string, List<string>> errors,
        string key,
        string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = [];
            errors[key] = messages;
        }

        messages.Add(message);
    }
}
