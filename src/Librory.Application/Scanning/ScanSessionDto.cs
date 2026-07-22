namespace Librory.Application.Scanning;

public sealed record ScanSessionDto(
    Guid ScanSessionId,
    Guid FamilyId,
    IReadOnlyList<ScanCandidateDto> Candidates,
    DateTimeOffset ExpiresAt);
