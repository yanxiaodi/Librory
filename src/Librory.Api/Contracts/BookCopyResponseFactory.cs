using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public static class BookCopyResponseFactory
{
    public static BookCopyResponse Create(BookCopy copy)
    {
        ArgumentNullException.ThrowIfNull(copy);

        return new BookCopyResponse(
            copy.Id,
            copy.FamilyId,
            copy.MemberId,
            copy.BookEditionId,
            copy.DuplicateStatus,
            copy.Condition,
            copy.PurchaseStore,
            copy.PurchasePrice,
            copy.ShelfLocation,
            copy.PurchasedAt,
            copy.IntakeNotes);
    }
}
