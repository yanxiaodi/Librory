namespace Librory.Api.Contracts;

public sealed record BookMetadataSearchResponse(
    string Query,
    int TotalItems,
    IReadOnlyList<BookMetadataCandidateResponse> Candidates);
