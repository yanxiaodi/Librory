namespace Librory.Application.Scanning;

public interface IScanPhotoStorage
{
    Task<string> StoreTemporaryAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken);

    Task DeleteAsync(string shelfPhotoPath, CancellationToken cancellationToken);
}
