using Librory.Application.Intake;
using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public sealed record ConfirmScanPurchaseRequest(
    Guid PurchaseRequestId,
    Guid OwnerMemberId,
    BookMetadataImportCandidateRequest? SelectedMetadata = null,
    DuplicateResolution DuplicateResolution = DuplicateResolution.NotSpecified,
    BookCopyDuplicateStatus DuplicateStatus = BookCopyDuplicateStatus.Unchecked,
    Guid? ExistingBookEditionId = null,
    Guid? ExistingBookWorkId = null,
    string? ManualTitle = null,
    string? ManualAuthor = null,
    string? Isbn = null,
    string? Format = null,
    int? PublicationYear = null,
    string? Condition = null,
    string? PurchaseStore = null,
    decimal? PurchasePrice = null,
    string? ShelfLocation = null,
    DateTimeOffset? PurchasedAt = null,
    string? IntakeNotes = null);
