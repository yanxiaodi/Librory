namespace Librory.Application.Scanning;

public sealed record ScanCandidateDto(
    Guid Id,
    string DisplayTitle,
    string? Author,
    decimal RecommendationScore,
    bool IsAlreadyOwned,
    string? DuplicateMessage,
    string ConfidenceLabel);
