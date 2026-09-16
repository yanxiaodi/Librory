namespace Librory.Api.Validation;

internal static class ScanCandidateValidation
{
    public const int MaxDisplayTitleLength = 300;
    public const int MaxAuthorLength = 300;
    public const int MaxDuplicateMessageLength = 1000;
    public const int MaxConfidenceLabelLength = 64;

    public static IReadOnlyDictionary<string, string[]> Validate(
        string? displayTitle,
        string? confidenceLabel,
        string? author,
        string? duplicateMessage,
        string prefix = "")
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        AddMaxLength(errors, Key(prefix, "displayTitle"), displayTitle, MaxDisplayTitleLength);
        AddMaxLength(errors, Key(prefix, "confidenceLabel"), confidenceLabel, MaxConfidenceLabelLength);
        AddMaxLength(errors, Key(prefix, "author"), author, MaxAuthorLength);
        AddMaxLength(errors, Key(prefix, "duplicateMessage"), duplicateMessage, MaxDuplicateMessageLength);
        return errors;
    }

    public static void Merge(
        IDictionary<string, string[]> target,
        IReadOnlyDictionary<string, string[]> source)
    {
        foreach (var (key, messages) in source)
        {
            target[key] = messages;
        }
    }

    private static void AddMaxLength(
        IDictionary<string, string[]> errors,
        string key,
        string? value,
        int maxLength)
    {
        if (value is not null && value.Length > maxLength)
        {
            errors[key] = [$"Value must be {maxLength} characters or fewer."];
        }
    }

    private static string Key(string prefix, string field)
    {
        return string.IsNullOrEmpty(prefix) ? field : $"{prefix}.{field}";
    }
}
