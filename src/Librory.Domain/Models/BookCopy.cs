namespace Librory.Domain.Models;

public sealed class BookCopy
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid FamilyId { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid BookEditionId { get; private set; }
    public string? Condition { get; set; }
    public string? PurchaseStore { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? ShelfLocation { get; set; }
    public DateTimeOffset? PurchasedAt { get; set; }
    public Family Family { get; private set; } = null!;
    public Member Member { get; private set; } = null!;
    public BookEdition BookEdition { get; private set; } = null!;

    public static BookCopy Create(
        BookEdition bookEdition,
        Family family,
        Member member,
        string? condition = null,
        string? purchaseStore = null,
        decimal? purchasePrice = null,
        string? shelfLocation = null,
        DateTimeOffset? purchasedAt = null)
    {
        ArgumentNullException.ThrowIfNull(bookEdition);
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(member);

        if (bookEdition.BookWorkId == Guid.Empty)
        {
            throw new InvalidOperationException("Edition must belong to a work before a copy can be created.");
        }

        if (member.FamilyId != family.Id)
        {
            throw new InvalidOperationException("Member must belong to the same family as the copy.");
        }

        var copy = new BookCopy
        {
            Condition = condition?.Trim(),
            PurchaseStore = purchaseStore?.Trim(),
            PurchasePrice = purchasePrice,
            ShelfLocation = shelfLocation?.Trim(),
            PurchasedAt = purchasedAt,
        };

        copy.AttachTo(bookEdition, family, member);
        return copy;
    }

    private void AttachTo(BookEdition bookEdition, Family family, Member member)
    {
        BookEdition = bookEdition;
        BookEditionId = bookEdition.Id;
        Family = family;
        FamilyId = family.Id;
        Member = member;
        MemberId = member.Id;
    }
}
