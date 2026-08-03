namespace Librory.Domain.Models;

public sealed class BookRecognitionJob
{
    private const int MaxSourcePhotoPathLength = 400;
    private const int MaxLanguageLength = 16;
    private const int MaxFailureMessageLength = 2_000;

    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid FamilyId { get; private set; }
    public string SourcePhotoPath { get; private set; } = string.Empty;
    public string? Language { get; private set; }
    public BookRecognitionJobStatus Status { get; private set; }
    public string? ResultJson { get; private set; }
    public string? FailureMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Family Family { get; private set; } = null!;

    public static BookRecognitionJob Create(Guid familyId, string sourcePhotoPath, string? language, DateTimeOffset createdAt)
    {
        if (familyId == Guid.Empty)
        {
            throw new ArgumentException("Family id is required.", nameof(familyId));
        }

        var normalizedPhotoPath = NormalizeSourcePhotoPath(sourcePhotoPath);
        var normalizedLanguage = NormalizeLanguage(language);

        return new BookRecognitionJob
        {
            FamilyId = familyId,
            SourcePhotoPath = normalizedPhotoPath,
            Language = normalizedLanguage,
            Status = BookRecognitionJobStatus.Queued,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };
    }

    public void MarkRunning(DateTimeOffset updatedAt)
    {
        Status = BookRecognitionJobStatus.Running;
        UpdatedAt = updatedAt;
        FailureMessage = null;
    }

    public void MarkSucceeded(string resultJson, DateTimeOffset updatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resultJson);

        Status = BookRecognitionJobStatus.Succeeded;
        ResultJson = resultJson.Trim();
        FailureMessage = null;
        UpdatedAt = updatedAt;
    }

    public void MarkFailed(string failureMessage, DateTimeOffset updatedAt)
    {
        var normalizedFailure = NormalizeFailureMessage(failureMessage);

        Status = BookRecognitionJobStatus.Failed;
        FailureMessage = normalizedFailure;
        UpdatedAt = updatedAt;
    }

    private static string NormalizeSourcePhotoPath(string sourcePhotoPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePhotoPath);

        var normalized = sourcePhotoPath.Trim();
        if (normalized.Length > MaxSourcePhotoPathLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourcePhotoPath),
                normalized,
                $"Source photo path must be {MaxSourcePhotoPathLength} characters or fewer.");
        }

        return normalized;
    }

    private static string? NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        var normalized = language.Trim();
        if (normalized.Length > MaxLanguageLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(language),
                normalized,
                $"Language must be {MaxLanguageLength} characters or fewer.");
        }

        return normalized;
    }

    private static string NormalizeFailureMessage(string failureMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureMessage);

        var normalized = failureMessage.Trim();
        if (normalized.Length > MaxFailureMessageLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failureMessage),
                normalized,
                $"Failure message must be {MaxFailureMessageLength} characters or fewer.");
        }

        return normalized;
    }
}
