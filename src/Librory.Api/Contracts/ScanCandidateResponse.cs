using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public sealed record ScanCandidateResponse(
    Guid Id,
    string DisplayTitle,
    string? Author,
    decimal? RecommendationScore,
    bool IsAlreadyOwned,
    string? DuplicateMessage,
    string ConfidenceLabel,
    PreferredLanguage? DetectedLanguage = null,
    int RecognitionRank = 0,
    ScanCandidateMetadataSnapshotResponse? MetadataSnapshot = null,
    PurchaseStatus PurchaseStatus = PurchaseStatus.Pending,
    Guid? PurchasedBookCopyId = null,
    Guid? PurchaseRequestId = null,
    DateTimeOffset? PurchasedAt = null);
