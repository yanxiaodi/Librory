using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public sealed record CreateBookCopyRequest(
    Guid? BookEditionId,
    BookCopyDuplicateStatus DuplicateStatus = BookCopyDuplicateStatus.Unchecked,
    string? Condition = null,
    string? PurchaseStore = null,
    decimal? PurchasePrice = null,
    string? ShelfLocation = null,
    DateTimeOffset? PurchasedAt = null,
    string? IntakeNotes = null);
