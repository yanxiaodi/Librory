using Librory.Domain.Models;

namespace Librory.Application.Wishlist;

public static class WishlistItemDtoFactory
{
    public static WishlistItemDto Create(WishlistItem wishlistItem)
    {
        ArgumentNullException.ThrowIfNull(wishlistItem);

        return new WishlistItemDto(
            wishlistItem.Id,
            wishlistItem.BookWorkId,
            wishlistItem.BookEditionId,
            wishlistItem.Title,
            wishlistItem.Author);
    }
}
