namespace Librory.Api.Contracts;

public sealed record CreateBookWorkRequest(
    string Title,
    string? Author = null,
    string? Isbn = null,
    string? Format = null,
    int? PublicationYear = null);
