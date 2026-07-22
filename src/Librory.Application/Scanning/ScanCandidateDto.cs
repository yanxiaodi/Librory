namespace Librory.Application.Scanning;

public sealed record ScanCandidateDto(
    Guid CandidateId,
    string DisplayTitle,
    string? Author,
    decimal RecommendationScore,
    bool IsAlreadyOwned,
    string? DuplicateMessage,
    string ConfidenceLabel);
