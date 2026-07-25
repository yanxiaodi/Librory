namespace Librory.Api.Contracts;

public sealed record CreateWishlistItemRequest(
    string Title,
    string? Author = null,
    Guid? BookWorkId = null,
    Guid? BookEditionId = null);
