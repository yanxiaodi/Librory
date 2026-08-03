namespace Librory.Application.Metadata;

public interface IBookMetadataImportService
{
    Task<BookMetadataImportResult> ImportAsync(
        BookMetadataCandidate candidate,
        CancellationToken cancellationToken);
}
