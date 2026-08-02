namespace Librory.Api.Contracts;

public sealed record BookMetadataCandidateResponse(
    string Source,
    string SourceId,
    string Title,
    string? Subtitle,
    IReadOnlyList<string> Authors,
    string? Publisher,
    string? PublishedDate,
    string? Language,
    string? Description,
    string? Isbn10,
    string? Isbn13,
    string? ThumbnailUrl,
    string? InfoUrl);
