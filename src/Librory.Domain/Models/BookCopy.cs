namespace Librory.Domain.Models;

public sealed class BookCopy
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid FamilyId { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid BookEditionId { get; private set; }
    public BookCopyDuplicateStatus DuplicateStatus { get; private set; } = BookCopyDuplicateStatus.Unchecked;
    public string? Condition { get; set; }
    public string? PurchaseStore { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? ShelfLocation { get; set; }
    public DateTimeOffset? PurchasedAt { get; set; }
    public string? IntakeNotes { get; set; }
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
        DateTimeOffset? purchasedAt = null,
        BookCopyDuplicateStatus duplicateStatus = BookCopyDuplicateStatus.Unchecked,
        string? intakeNotes = null)
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
            DuplicateStatus = duplicateStatus,
            Condition = string.IsNullOrWhiteSpace(condition) ? null : condition.Trim(),
            PurchaseStore = string.IsNullOrWhiteSpace(purchaseStore) ? null : purchaseStore.Trim(),
            PurchasePrice = purchasePrice,
            ShelfLocation = string.IsNullOrWhiteSpace(shelfLocation) ? null : shelfLocation.Trim(),
            PurchasedAt = purchasedAt,
            IntakeNotes = string.IsNullOrWhiteSpace(intakeNotes) ? null : intakeNotes.Trim(),
        };

        copy.AttachTo(bookEdition, family, member);
        return copy;
    }

    public void ConfirmDuplicateStatus(BookCopyDuplicateStatus duplicateStatus)
    {
        if (duplicateStatus == BookCopyDuplicateStatus.Unchecked)
        {
            throw new InvalidOperationException("Duplicate status cannot be reset to unchecked.");
        }

        DuplicateStatus = duplicateStatus;
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
