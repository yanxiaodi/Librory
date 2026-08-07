using Librory.Domain.Models;

namespace Librory.Application.Scanning;

public static class ScanCandidateDtoFactory
{
    public static ScanCandidateDto Create(Family family, ScanCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(candidate);

        var duplicateDetection = family.DetectPotentialDuplicate(candidate.DisplayTitle);

        return new ScanCandidateDto(
            candidate.Id,
            candidate.DisplayTitle,
            candidate.Author,
            candidate.RecommendationScore,
            candidate.IsAlreadyOwned || duplicateDetection.HasPotentialDuplicate,
            candidate.DuplicateMessage ?? duplicateDetection.FollowUpHint,
            candidate.ConfidenceLabel,
            candidate.DetectedLanguage);
    }
}
