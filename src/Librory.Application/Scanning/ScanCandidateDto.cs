using Librory.Domain.Models;

namespace Librory.Application.Scanning;

public sealed record ScanCandidateDto(
    Guid Id,
    string DisplayTitle,
    string? Author,
    decimal? RecommendationScore,
    bool IsAlreadyOwned,
    string? DuplicateMessage,
    string ConfidenceLabel,
    PreferredLanguage? DetectedLanguage = null,
    int RecognitionRank = 0,
    ScanCandidateMetadataSnapshot? MetadataSnapshot = null,
    PurchaseStatus PurchaseStatus = PurchaseStatus.Pending,
    Guid? PurchasedBookCopyId = null,
    Guid? PurchaseRequestId = null,
    DateTimeOffset? PurchasedAt = null);
