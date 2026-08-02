namespace Librory.Application.Metadata;

public sealed record BookMetadataSearchResult(
    string Query,
    int TotalItems,
    IReadOnlyList<BookMetadataCandidate> Candidates);
