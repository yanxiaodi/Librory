namespace Librory.Application.Scanning;

public sealed record ScanShelfRequest(
    Guid FamilyId,
    string? PreferredLanguage,
    string ShelfPhotoPath,
    TimeSpan? RetentionWindow = null,
    IReadOnlyList<ScanCandidateInput>? Candidates = null,
    Guid? TargetMemberId = null);
