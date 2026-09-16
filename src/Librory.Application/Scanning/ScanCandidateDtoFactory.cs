using Librory.Domain.Models;

namespace Librory.Application.Scanning;

public static class ScanCandidateDtoFactory
{
    public static ScanCandidateDto Create(Family family, ScanCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(candidate);

        var duplicateDetection = candidate.PurchaseStatus == PurchaseStatus.Purchased
            ? null
            : family.DetectPotentialDuplicate(candidate.DisplayTitle);
        var metadataSnapshot = ScanCandidateMetadataSnapshotSerializer.Deserialize(candidate.MetadataMatchesJson);

        return new ScanCandidateDto(
            candidate.Id,
            candidate.DisplayTitle,
            candidate.Author,
            null,
            candidate.IsAlreadyOwned || duplicateDetection?.HasPotentialDuplicate == true,
            candidate.DuplicateMessage ?? duplicateDetection?.FollowUpHint,
            candidate.ConfidenceLabel,
            candidate.DetectedLanguage,
            candidate.RecognitionRank,
            metadataSnapshot,
            candidate.PurchaseStatus,
            candidate.PurchasedBookCopyId,
            candidate.PurchaseRequestId,
            candidate.PurchasedAt);
    }
}
