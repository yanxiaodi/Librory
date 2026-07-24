using Librory.Domain.Models;

namespace Librory.Application.Wishlist;

public static class WishlistRecorder
{
    public static WishlistItem Record(Family family, WishlistItemRequest request)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(request);

        return family.AddWishlistItem(
            request.Title,
            request.Author,
            request.BookWork,
            request.BookEdition);
    }
}
