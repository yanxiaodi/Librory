using Librory.Application.Metadata;
using Librory.Domain.Models;

namespace Librory.Application.Intake;

public sealed record ScanPurchaseRequest(
    Guid ScanSessionId,
    Guid CandidateId,
    Guid PurchaseRequestId,
    Guid OwnerMemberId,
    BookMetadataCandidate? SelectedMetadata = null,
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
