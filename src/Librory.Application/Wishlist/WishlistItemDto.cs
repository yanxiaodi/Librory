namespace Librory.Application.Wishlist;

public sealed record WishlistItemDto(
    Guid WishlistItemId,
    Guid? BookWorkId,
    Guid? BookEditionId,
    string Title,
    string? Author);
