namespace Librory.Api.Contracts;

public sealed record BookRecognitionCandidateResponse(
    Guid CandidateId,
    string DisplayTitle,
    string EvidenceText,
    int Rank,
    IReadOnlyList<BookMetadataCandidateResponse> MetadataMatches);
