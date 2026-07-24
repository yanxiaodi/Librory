using Librory.Domain.Models;

namespace Librory.Application.Intake;

public static class ManualBookIntakeRecorder
{
    public static BookCopy Record(Family family, ManualBookIntakeRequest request)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(request);

        return family.AddBookCopy(
            request.Edition,
            request.OwningMember,
            request.Condition,
            request.PurchaseStore,
            request.PurchasePrice,
            request.ShelfLocation,
            request.PurchasedAt,
            request.DuplicateStatus,
            request.IntakeNotes);
    }
}
