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
            candidates,
            session.ExpiresAt);
    }
}
