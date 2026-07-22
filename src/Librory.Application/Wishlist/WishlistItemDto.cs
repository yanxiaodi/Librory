namespace Librory.Application.Wishlist;

public sealed record WishlistItemDto(
    Guid WishlistItemId,
    Guid? BookWorkId,
    string Title,
    string? Author);
