namespace Librory.Api.Contracts;

public sealed record ScanSessionResponse(
    Guid ScanSessionId,
    Guid FamilyId,
    string ShelfPhotoPath,
    IReadOnlyList<ScanCandidateResponse> Candidates,
    DateTimeOffset ExpiresAt);
