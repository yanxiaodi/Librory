using Librory.Domain.Models;

namespace Librory.Application.Scanning;

public static class ScanSessionDtoFactory
{
    public static ScanSessionDto Create(Family family, ScanSession session)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(session);

        var candidates = session.Candidates
            .Select(candidate => ScanCandidateDtoFactory.Create(family, candidate))
            .ToList();

        return new ScanSessionDto(
            session.Id,
            session.FamilyId,
            session.ShelfPhotoPath,
            candidates,
            session.ExpiresAt,
            session.TargetMemberId,
            family.Members.SingleOrDefault(member => member.Id == session.TargetMemberId)?.DisplayName ?? string.Empty,
            session.TargetProfileAvailable,
            session.TargetProfileUsed,
            session.InferredLanguage,
            session.HasMixedLanguages);
    }
}
