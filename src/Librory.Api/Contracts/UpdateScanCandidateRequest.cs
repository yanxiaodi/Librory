using Librory.Application.Metadata;

namespace Librory.Api.Contracts;

public sealed record UpdateScanCandidateRequest(
    string DisplayTitle,
    string ConfidenceLabel,
    string? Author = null,
    decimal? RecommendationScore = null,
    bool IsAlreadyOwned = false,
    string? DuplicateMessage = null,
    string? RecognitionEvidence = null,
    int RecognitionRank = 0,
    IReadOnlyList<BookMetadataImportCandidateRequest>? MetadataMatches = null);
