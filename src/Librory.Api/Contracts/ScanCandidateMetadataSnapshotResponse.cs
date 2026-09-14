namespace Librory.Api.Contracts;

public sealed record ScanCandidateMetadataSnapshotResponse(
    int SchemaVersion,
    string? EvidenceText,
    IReadOnlyList<BookMetadataCandidateResponse> Matches);
