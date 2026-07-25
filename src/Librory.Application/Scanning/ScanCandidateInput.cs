namespace Librory.Application.Scanning;

public sealed record ScanCandidateInput(
    string DisplayTitle,
    string ConfidenceLabel,
    string? Author = null,
    decimal RecommendationScore = 0m,
    bool IsAlreadyOwned = false,
    string? DuplicateMessage = null);
