using Librory.Application.Metadata;

namespace Librory.Application.Scanning;

public sealed record CorrectionRequest(
    string DisplayTitle,
    string ConfidenceLabel,
    string? Author = null,
    decimal RecommendationScore = 0m,
    bool IsAlreadyOwned = false,
    string? DuplicateMessage = null,
    string? RecognitionEvidence = null,
    int RecognitionRank = 0,
    IReadOnlyList<BookMetadataCandidate>? MetadataMatches = null);
