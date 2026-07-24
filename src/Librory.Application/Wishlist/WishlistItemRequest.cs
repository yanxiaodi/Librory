using Librory.Domain.Models;

namespace Librory.Application.Wishlist;

public sealed class WishlistItemRequest
{
    public WishlistItemRequest(
        string title,
        string? author = null,
        BookWork? bookWork = null,
        BookEdition? bookEdition = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title.Trim();
        Author = Normalize(author);
        BookWork = bookWork;
        BookEdition = bookEdition;
    }

    public string Title { get; }

    public string? Author { get; }

    public BookWork? BookWork { get; }

    public BookEdition? BookEdition { get; }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
