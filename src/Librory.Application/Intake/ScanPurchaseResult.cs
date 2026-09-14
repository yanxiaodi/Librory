using Librory.Domain.Models;

namespace Librory.Application.Intake;

public sealed record ScanPurchaseResult(
    BookCopy Copy,
    BookWork Work,
    BookEdition Edition,
    DuplicateDetectionResult DuplicateDetection,
    bool IsReplay);

public sealed class DuplicateConfirmationRequiredException : InvalidOperationException
{
    public DuplicateConfirmationRequiredException(DuplicateDetectionResult duplicateDetection)
        : base("A duplicate confirmation is required before this book can be purchased.")
    {
        DuplicateDetection = duplicateDetection;
    }

    public DuplicateDetectionResult DuplicateDetection { get; }
}
