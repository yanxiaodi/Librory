namespace Librory.Application.Scanning;

public sealed record ScanSessionDto(
    Guid ScanSessionId,
    Guid FamilyId,
    string ShelfPhotoPath,
    IReadOnlyList<ScanCandidateDto> Candidates,
    DateTimeOffset ExpiresAt);
