namespace Librory.Domain.Models;

public sealed class WishlistItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid FamilyId { get; private set; }
    public Guid? BookWorkId { get; private set; }
    public Guid? BookEditionId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Author { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public Family Family { get; private set; } = null!;
    public BookWork? BookWork { get; private set; }
    public BookEdition? BookEdition { get; private set; }

    public static WishlistItem Create(
        Family family,
        string title,
        string? author = null,
        BookWork? bookWork = null,
        BookEdition? bookEdition = null)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var resolvedBookWork = bookWork ?? bookEdition?.BookWork;

        if (bookEdition is not null && !bookEdition.IsAttachedToWork)
        {
            throw new InvalidOperationException("Edition must belong to a work before it can be added to the wishlist.");
        }

        if (bookEdition is not null && resolvedBookWork is not null && bookEdition.BookWorkId != resolvedBookWork.Id)
        {
            throw new InvalidOperationException("Edition must belong to the same work as the wishlist item.");
        }

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

        return item;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
