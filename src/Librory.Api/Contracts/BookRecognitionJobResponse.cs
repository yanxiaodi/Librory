using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public sealed record BookRecognitionJobResponse(
    Guid JobId,
    Guid FamilyId,
    BookRecognitionJobStatus Status,
    string SourcePhotoPath,
    IReadOnlyList<BookRecognitionCandidateResponse> Candidates,
    IReadOnlyList<string> Warnings,
    string? FailureMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
