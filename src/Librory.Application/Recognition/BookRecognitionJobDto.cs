using Librory.Domain.Models;

namespace Librory.Application.Recognition;

public sealed record BookRecognitionJobDto(
    Guid JobId,
    Guid FamilyId,
    BookRecognitionJobStatus Status,
    string SourcePhotoPath,
    IReadOnlyList<BookRecognitionCandidateDto> Candidates,
    IReadOnlyList<string> Warnings,
    string? FailureMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
