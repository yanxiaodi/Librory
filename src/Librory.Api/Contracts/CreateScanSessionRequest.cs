namespace Librory.Api.Contracts;

public sealed record CreateScanSessionRequest(
    string ShelfPhotoPath,
    int? RetentionWindowDays = null,
    IReadOnlyList<CreateScanCandidateRequest>? Candidates = null);
