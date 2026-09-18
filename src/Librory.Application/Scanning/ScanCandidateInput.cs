using Librory.Application.Metadata;
using Librory.Domain.Models;

namespace Librory.Application.Scanning;

public sealed record ScanCandidateInput(
    string DisplayTitle,
    string ConfidenceLabel,
    string? Author = null,
    decimal? RecommendationScore = null,
    bool IsAlreadyOwned = false,
    string? DuplicateMessage = null,
    PreferredLanguage? DetectedLanguage = null,
    string? RecognitionEvidence = null,
    int RecognitionRank = 0,
    IReadOnlyList<BookMetadataCandidate>? MetadataMatches = null);
