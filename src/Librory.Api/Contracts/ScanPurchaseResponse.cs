using Librory.Application.Intake;
using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public sealed record ScanPurchaseResponse(
    BookCopyResponse Copy,
    BookWorkResponse Work,
    Guid BookEditionId,
    bool IsProvisional,
    BookCopyDuplicateStatus DuplicateStatus,
    bool IsReplay);

public sealed record DuplicateConfirmationResponse(
    string Message,
    string? FollowUpHint,
    IReadOnlyList<DuplicateMatchResponse> Matches);

public sealed record DuplicateMatchResponse(
    Guid BookCopyId,
    Guid BookEditionId,
    Guid BookWorkId,
    string Title,
    string? Isbn,
    string? Format,
    int? PublicationYear);
