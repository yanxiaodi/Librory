namespace Librory.Api.Contracts;

public sealed record ResolveScanCandidateRequest(
    string Title,
    string? Author = null,
    Guid? BookWorkId = null,
    string? Isbn = null,
    string? Format = null,
    int? PublicationYear = null);
