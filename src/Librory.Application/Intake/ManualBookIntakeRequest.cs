using Librory.Domain.Models;

namespace Librory.Application.Intake;

public sealed class ManualBookIntakeRequest
{
    public ManualBookIntakeRequest(
        BookEdition edition,
        Member owningMember,
        BookCopyDuplicateStatus duplicateStatus,
        string? condition = null,
        string? purchaseStore = null,
        decimal? purchasePrice = null,
        string? shelfLocation = null,
        DateTimeOffset? purchasedAt = null,
        string? intakeNotes = null)
    {
        ArgumentNullException.ThrowIfNull(edition);
        ArgumentNullException.ThrowIfNull(owningMember);

        Edition = edition;
        OwningMember = owningMember;
        Condition = Normalize(condition);
        PurchaseStore = Normalize(purchaseStore);
        PurchasePrice = purchasePrice;
        ShelfLocation = Normalize(shelfLocation);
        PurchasedAt = purchasedAt;
        DuplicateStatus = duplicateStatus;
        IntakeNotes = Normalize(intakeNotes);
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

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
