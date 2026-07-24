using Librory.Domain.Models;

namespace Librory.Application.Intake;

public sealed record ManualBookIntakeResult(
    BookCopy Copy,
    DuplicateDetectionResult DuplicateDetection)
{
    public bool HasPotentialDuplicate => DuplicateDetection.HasPotentialDuplicate;

    public string? DuplicateWarning => DuplicateDetection.FollowUpHint;
}
