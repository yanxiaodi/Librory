namespace Librory.Api.Contracts;

public sealed record BookWorkResponse(
    Guid BookWorkId,
    string Title,
    string? Author,
    IReadOnlyList<BookEditionResponse> Editions);
