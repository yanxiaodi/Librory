using Librory.Domain.Models;

namespace Librory.Application.Scanning;

public static class ScanSessionRecorder
{
    public static ScanSession Record(Family family, ScanShelfRequest request, ScanTargetContext targetContext)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(targetContext);

        if (request.FamilyId != family.Id)
        {
            throw new InvalidOperationException("Scan request family id must match the loaded family.");
        }

        var session = ScanSession.Create(
            family,
            targetContext.TargetMemberId,
            targetContext.TargetProfileAvailable,
            targetContext.TargetProfileUsed,
            request.ShelfPhotoPath,
            request.RetentionWindow);
        family.ScanSessions.Add(session);
        foreach (var candidateInput in request.Candidates ?? [])
        {
            session.AddCandidate(CreateCandidate(family, candidateInput));
        }

        return session;
    }

    private static ScanCandidate CreateCandidate(Family family, ScanCandidateInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var duplicateDetection = family.DetectPotentialDuplicate(input.DisplayTitle);
        var duplicateMessage = input.DuplicateMessage ?? duplicateDetection.FollowUpHint;

        return ScanCandidate.Create(
            input.DisplayTitle,
            input.ConfidenceLabel,
            input.Author,
            input.RecommendationScore,
            input.IsAlreadyOwned || duplicateDetection.HasPotentialDuplicate,
            duplicateMessage,
            input.DetectedLanguage);
    }
}
