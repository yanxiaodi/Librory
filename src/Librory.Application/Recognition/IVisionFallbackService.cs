namespace Librory.Application.Recognition;

public interface IVisionFallbackService
{
    Task<IReadOnlyList<string>> SuggestCandidateTitlesAsync(
        string sourcePhotoPath,
        IReadOnlyList<RecognizedTextBlock> recognizedText,
        CancellationToken cancellationToken);
}
