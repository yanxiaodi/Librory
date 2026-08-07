using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public sealed record CreateScanCandidateRequest(
    string DisplayTitle,
    string ConfidenceLabel,
    string? Author = null,
    decimal RecommendationScore = 0m,
    bool IsAlreadyOwned = false,
    string? DuplicateMessage = null,
    PreferredLanguage? DetectedLanguage = null);
