namespace Librory.Application.Recognition;

public interface IBookRecognitionJobService
{
    Task<BookRecognitionJobDto> CreateAsync(
        Guid familyId,
        string sourcePhotoPath,
        string? language,
        CancellationToken cancellationToken);

    Task<BookRecognitionJobDto?> GetAsync(
        Guid familyId,
        Guid jobId,
        CancellationToken cancellationToken);
}
