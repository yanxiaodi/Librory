namespace Librory.Application.Recognition;

public interface IBookRecognitionPipeline
{
    Task<BookRecognitionJobResult> RecognizeAsync(
        string sourcePhotoPath,
        string? language,
        CancellationToken cancellationToken);
}
