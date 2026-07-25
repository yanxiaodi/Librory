namespace Librory.Api.Contracts;

public sealed record ScanCandidateResponse(
    Guid Id,
    string DisplayTitle,
    string? Author,
    decimal RecommendationScore,
    bool IsAlreadyOwned,
    string? DuplicateMessage,
    string ConfidenceLabel);
