namespace Librory.Api.Contracts;

public sealed record BookEditionResponse(
    Guid BookEditionId,
    string? Isbn,
    string? Format,
    int? PublicationYear);
