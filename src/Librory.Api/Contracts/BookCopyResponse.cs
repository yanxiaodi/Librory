using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public sealed record BookCopyResponse(
    Guid BookCopyId,
    Guid FamilyId,
    Guid MemberId,
    Guid BookEditionId,
    BookCopyDuplicateStatus DuplicateStatus,
    string? Condition,
    string? PurchaseStore,
    decimal? PurchasePrice,
    string? ShelfLocation,
    DateTimeOffset? PurchasedAt,
    string? IntakeNotes);
