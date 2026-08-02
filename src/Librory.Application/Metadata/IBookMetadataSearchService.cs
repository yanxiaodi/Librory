namespace Librory.Application.Metadata;

public interface IBookMetadataSearchService
{
    Task<BookMetadataSearchResult> SearchByTitleAsync(
        string title,
        string? language,
        int maxResults,
        CancellationToken cancellationToken);
}
