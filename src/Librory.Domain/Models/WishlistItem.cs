namespace Librory.Domain.Models;

public sealed class WishlistItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid FamilyId { get; set; }
    public Guid? BookWorkId { get; set; }
    public Guid? BookEditionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public Family Family { get; set; } = null!;
    public BookWork? BookWork { get; set; }
    public BookEdition? BookEdition { get; set; }

    public static WishlistItem Create(
        Family family,
        string title,
        string? author = null,
        BookWork? bookWork = null,
        BookEdition? bookEdition = null)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (bookEdition is not null && !bookEdition.IsAttachedToWork)
        {
            throw new InvalidOperationException("Edition must belong to a work before it can be added to the wishlist.");
        }

        if (bookEdition is not null && bookWork is not null && bookEdition.BookWorkId != Guid.Empty && bookEdition.BookWorkId != bookWork.Id)
        {
            throw new InvalidOperationException("Edition must belong to the same work as the wishlist item.");
        }

        var resolvedBookWork = bookWork ?? bookEdition?.BookWork;

        var item = new WishlistItem
        {
            Title = title.Trim(),
            Author = Normalize(author),
            BookWork = resolvedBookWork,
            BookWorkId = resolvedBookWork?.Id,
            BookEdition = bookEdition,
            BookEditionId = bookEdition?.Id,
            Family = family,
            FamilyId = family.Id,
        };

        family.RegisterWishlistItem(item);
        return item;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
