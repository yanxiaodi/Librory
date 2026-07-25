namespace Librory.Api.Contracts;

public sealed record UpdateScanCandidateRequest(
    string DisplayTitle,
    string ConfidenceLabel,
    string? Author = null,
    decimal RecommendationScore = 0m,
    bool IsAlreadyOwned = false,
    string? DuplicateMessage = null);
