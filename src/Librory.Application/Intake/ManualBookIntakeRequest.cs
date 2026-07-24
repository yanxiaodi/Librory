using Librory.Domain.Models;

namespace Librory.Application.Intake;

public sealed class ManualBookIntakeRequest
{
    public ManualBookIntakeRequest(
        BookEdition edition,
        Member owningMember,
        string? condition = null,
        string? purchaseStore = null,
        decimal? purchasePrice = null,
        string? shelfLocation = null,
        DateTimeOffset? purchasedAt = null,
        BookCopyDuplicateStatus duplicateStatus = BookCopyDuplicateStatus.Unchecked,
        string? intakeNotes = null)
    {
        ArgumentNullException.ThrowIfNull(edition);
        ArgumentNullException.ThrowIfNull(owningMember);

        Edition = edition;
        OwningMember = owningMember;
        Condition = condition;
        PurchaseStore = purchaseStore;
        PurchasePrice = purchasePrice;
        ShelfLocation = shelfLocation;
        PurchasedAt = purchasedAt;
        DuplicateStatus = duplicateStatus;
        IntakeNotes = intakeNotes;
    }

    public BookEdition Edition { get; }

    public Member OwningMember { get; }

    public string? Condition { get; }

    public string? PurchaseStore { get; }

    public decimal? PurchasePrice { get; }

    public string? ShelfLocation { get; }

    public DateTimeOffset? PurchasedAt { get; }

    public BookCopyDuplicateStatus DuplicateStatus { get; }

    public string? IntakeNotes { get; }
}
