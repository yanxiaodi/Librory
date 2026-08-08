namespace Librory.Application.Recognition;

/// <summary>
/// Persisted recognition payload for a completed book recognition job.
/// </summary>
public sealed record BookRecognitionJobResult(
    string SourcePhotoPath,
    IReadOnlyList<BookRecognitionCandidateDto> Candidates,
    IReadOnlyList<string> Warnings);
