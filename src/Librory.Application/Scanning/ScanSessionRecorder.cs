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
            session.AddCandidate(CreateCandidate(candidateInput));
        }

        return session;
    }

    private static ScanCandidate CreateCandidate(ScanCandidateInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return ScanCandidate.Create(
            input.DisplayTitle,
            input.ConfidenceLabel,
            input.Author,
            input.RecommendationScore,
            input.IsAlreadyOwned,
            input.DuplicateMessage,
            input.DetectedLanguage);
    }
}
