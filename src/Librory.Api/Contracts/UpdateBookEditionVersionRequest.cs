namespace Librory.Api.Contracts;

public sealed record UpdateBookEditionVersionRequest(
    string? Isbn = null,
    string? Format = null,
    int? PublicationYear = null);
