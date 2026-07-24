using Librory.Domain.Models;

namespace Librory.Application.Intake;

public static class ManualBookIntakeRecorder
{
    public static BookCopy Record(Family family, ManualBookIntakeRequest request)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(request);

        return RecordWithDuplicateDetection(family, request).Copy;
    }

    public static ManualBookIntakeResult RecordWithDuplicateDetection(Family family, ManualBookIntakeRequest request)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(request);

        var duplicateDetection = family.DetectPotentialDuplicate(request.Edition);
        var copy = family.AddBookCopy(
            request.Edition,
            request.OwningMember,
            request.Condition,
            request.PurchaseStore,
            request.PurchasePrice,
            request.ShelfLocation,
            request.PurchasedAt,
            request.DuplicateStatus,
            request.IntakeNotes);

        return new ManualBookIntakeResult(copy, duplicateDetection);
    }
}
