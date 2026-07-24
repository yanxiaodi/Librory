namespace Librory.Domain.Models;

public enum BookCopyDuplicateStatus
{
    Unchecked = 0,
    ConfirmedUnique = 1,
    ConfirmedDuplicate = 2,
}
