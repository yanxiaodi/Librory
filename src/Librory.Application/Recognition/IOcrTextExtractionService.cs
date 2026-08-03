namespace Librory.Application.Recognition;

public interface IOcrTextExtractionService
{
    Task<IReadOnlyList<RecognizedTextBlock>> ExtractAsync(
        string sourcePhotoPath,
        CancellationToken cancellationToken);
}
