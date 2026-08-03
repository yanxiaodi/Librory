namespace Librory.Application.Recognition;

public sealed record BookRecognitionJobResult(
    string SourcePhotoPath,
    IReadOnlyList<BookRecognitionCandidateDto> Candidates,
    IReadOnlyList<string> Warnings);
