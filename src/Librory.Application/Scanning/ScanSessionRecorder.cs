using Librory.Domain.Models;

namespace Librory.Application.Scanning;

public static class ScanSessionRecorder
{
    public static ScanSession Record(Family family, ScanShelfRequest request)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(request);

        var session = family.StartScanSession(request.ShelfPhotoPath, request.RetentionWindow);
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
            input.DuplicateMessage);
    }
}
