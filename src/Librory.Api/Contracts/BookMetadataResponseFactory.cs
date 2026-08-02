using Librory.Application.Metadata;

namespace Librory.Api.Contracts;

public static class BookMetadataResponseFactory
{
    public static BookMetadataSearchResponse Create(BookMetadataSearchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var candidates = result.Candidates
            .Select(candidate => new BookMetadataCandidateResponse(
                candidate.Source,
                candidate.SourceId,
                candidate.Title,
                candidate.Subtitle,
                candidate.Authors,
                candidate.Publisher,
                candidate.PublishedDate,
                candidate.Language,
                candidate.Description,
                candidate.Isbn10,
                candidate.Isbn13,
                candidate.ThumbnailUrl,
                candidate.InfoUrl))
            .ToList();

        return new BookMetadataSearchResponse(result.Query, result.TotalItems, candidates);
    }
}
