using System.Data;
using Librory.Application.Families;
using Librory.Application.Intake;
using Librory.Application.Metadata;
using Librory.Application.Scanning;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Librory.Infrastructure.Intake;

public sealed class ScanPurchaseService : IScanPurchaseService
{
    private const int MaxRetryCount = 2;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentFamilyContextAccessor _currentFamilyContextAccessor;

    public ScanPurchaseService(
        IServiceScopeFactory scopeFactory,
        ICurrentFamilyContextAccessor currentFamilyContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(currentFamilyContextAccessor);

        _scopeFactory = scopeFactory;
        _currentFamilyContextAccessor = currentFamilyContextAccessor;
    }

    public async Task<ScanPurchaseResult> PurchaseAsync(
        ScanPurchaseRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        var current = _currentFamilyContextAccessor.Current
            ?? throw new UnauthorizedAccessException("Current family context is required.");

        Exception? lastRetryableFailure = null;
        for (var retry = 0; retry <= MaxRetryCount; retry++)
        {
            try
            {
                return await ExecuteAttemptAsync(request, current, cancellationToken);
            }
            catch (Exception exception) when (
                IsRetryablePostgresFailure(exception)
                || IsPurchaseRequestUniqueConstraint(exception))
            {
                // The failed scope and transaction are disposed by ExecuteAttemptAsync.
                // The next attempt intentionally resolves a new DbContext and importer.
                lastRetryableFailure = exception;
            }
        }

        throw new ScanPurchaseRetryableException(
            "The purchase could not be completed because the database transaction was repeatedly contended. Retry the purchase.",
            lastRetryableFailure!);
    }

    private async Task<ScanPurchaseResult> ExecuteAttemptAsync(
        ScanPurchaseRequest request,
        CurrentFamilyContext current,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();
        var importer = scope.ServiceProvider.GetRequiredService<IBookMetadataImportService>();
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var family = await LoadFamilyAsync(db, current.FamilyId, cancellationToken)
            ?? throw new KeyNotFoundException("Family not found.");
        var purchaser = family.Members.SingleOrDefault(member => member.Id == current.MemberId);
        if (purchaser is null || !purchaser.IsActive)
        {
            throw new UnauthorizedAccessException("The current family member is not active.");
        }

        var session = await db.ScanSessions
            .Include(scan => scan.Candidates)
            .SingleOrDefaultAsync(
                scan => scan.Id == request.ScanSessionId && scan.FamilyId == current.FamilyId,
                cancellationToken);
        if (session is null || session.IsExpired())
        {
            throw new KeyNotFoundException("Scan session not found.");
        }

        var candidate = session.Candidates.SingleOrDefault(item => item.Id == request.CandidateId);
        if (candidate is null)
        {
            throw new KeyNotFoundException("Scan candidate not found.");
        }

        var existingRequest = await db.ScanCandidates
            .SingleOrDefaultAsync(
                item => item.PurchaseRequestId == request.PurchaseRequestId && item.Id != candidate.Id,
                cancellationToken);
        if (existingRequest is not null)
        {
            throw new InvalidOperationException("This purchase request id has already been used for another scan candidate.");
        }

        if (candidate.PurchaseStatus == PurchaseStatus.Purchased)
        {
            if (candidate.PurchaseRequestId != request.PurchaseRequestId || !candidate.PurchasedBookCopyId.HasValue)
            {
                throw new InvalidOperationException("This scan candidate has already been purchased.");
            }

            var previousCopy = family.BookCopies.SingleOrDefault(copy => copy.Id == candidate.PurchasedBookCopyId.Value);
            if (previousCopy is null)
            {
                throw new InvalidOperationException("The purchased book copy could not be restored.");
            }

            await LoadWorkEditionsAsync(db, previousCopy.BookEdition.BookWorkId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new ScanPurchaseResult(
                previousCopy,
                previousCopy.BookEdition.BookWork,
                previousCopy.BookEdition,
                family.DetectPotentialDuplicate(previousCopy.BookEdition),
                true);
        }

        var owner = family.Members.SingleOrDefault(member => member.Id == request.OwnerMemberId);
        if (owner is null)
        {
            throw new KeyNotFoundException("Owner member not found in the current family.");
        }

        var purchaseMetadata = request.SelectedMetadata
            ?? (string.IsNullOrWhiteSpace(request.ManualTitle) ? null : CreateManualMetadata(request));
        var edition = await ResolveEditionAsync(db, importer, request, purchaseMetadata, cancellationToken);
        var duplicateDetection = family.DetectPotentialDuplicate(edition);
        ValidateSelectedResolution(request, duplicateDetection);
        var duplicateStatus = ResolveDuplicateStatus(request, duplicateDetection);
        var purchasedAt = request.PurchasedAt ?? DateTimeOffset.UtcNow;
        var intake = ManualBookIntakeRecorder.RecordWithDuplicateDetection(
            family,
            new ManualBookIntakeRequest(
                edition,
                owner,
                duplicateStatus,
                request.Condition,
                request.PurchaseStore,
                request.PurchasePrice,
                request.ShelfLocation,
                purchasedAt,
                request.IntakeNotes,
                purchaser));

        if (purchaseMetadata is not null)
        {
            var existingSnapshot = ScanCandidateMetadataSnapshotSerializer.Deserialize(candidate.MetadataMatchesJson);
            IReadOnlyList<BookMetadataCandidate> persistedMatches = request.SelectedMetadata is null
                ? []
                : [purchaseMetadata];
            candidate.ApplyPurchaseMetadata(purchaseMetadata.Title, purchaseMetadata.Authors.FirstOrDefault());
            candidate.ReplaceMetadataMatches(
                ScanCandidateMetadataSnapshotSerializer.Serialize(persistedMatches, existingSnapshot?.EvidenceText),
                resetReviewState: false);
        }

        candidate.MarkPurchased(intake.Copy.Id, request.PurchaseRequestId, purchasedAt);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsPurchaseRequestUniqueConstraint(exception))
        {
            throw new InvalidOperationException(
                "This purchase request id has already been used for another scan candidate.",
                exception);
        }
        await LoadWorkEditionsAsync(db, edition.BookWorkId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ScanPurchaseResult(intake.Copy, edition.BookWork, edition, intake.DuplicateDetection, false);
    }

    private static async Task<BookEdition> ResolveEditionAsync(
        LibroryDbContext db,
        IBookMetadataImportService importer,
        ScanPurchaseRequest request,
        BookMetadataCandidate? purchaseMetadata,
        CancellationToken cancellationToken)
    {
        ValidateResolutionShape(request);

        if (request.DuplicateResolution == DuplicateResolution.ExistingEdition)
        {
            var existingEdition = await db.BookEditions
                .Include(edition => edition.BookWork)
                    .ThenInclude(work => work.Editions)
                .SingleOrDefaultAsync(edition => edition.Id == request.ExistingBookEditionId!.Value, cancellationToken);
            return existingEdition
                ?? throw new KeyNotFoundException("Selected book edition not found.");
        }

        if (request.DuplicateResolution == DuplicateResolution.SameWorkNewEdition)
        {
            var existingWork = await db.BookWorks
                .Include(work => work.Editions)
                .SingleOrDefaultAsync(work => work.Id == request.ExistingBookWorkId!.Value, cancellationToken);
            if (existingWork is null)
            {
                throw new KeyNotFoundException("Selected book work not found.");
            }

            var targetMetadata = purchaseMetadata ?? CreateManualMetadata(request);
            var targetImportResult = await importer.ImportAsync(
                targetMetadata,
                cancellationToken,
                new BookMetadataImportOptions(
                    AllowProvisionalEdition: true,
                    Isbn: SelectIsbn(request),
                    Format: request.Format,
                    PublicationYear: request.PublicationYear,
                    ReuseExistingEditionByIsbn: false,
                    TargetWork: existingWork));
            return targetImportResult.Edition
                ?? throw new InvalidOperationException("Metadata import did not create a book edition.");
        }

        var metadata = purchaseMetadata ?? CreateManualMetadata(request);
        var importResult = await importer.ImportAsync(
            metadata,
            cancellationToken,
            new BookMetadataImportOptions(
                AllowProvisionalEdition: true,
                Isbn: SelectIsbn(request),
                Format: request.Format,
                PublicationYear: request.PublicationYear,
                ReuseExistingEditionByIsbn: false));

        return importResult.Edition
            ?? throw new InvalidOperationException("Metadata import did not create a book edition.");
    }

    private static BookMetadataCandidate CreateManualMetadata(ScanPurchaseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ManualTitle))
        {
            throw new ArgumentException("Selected metadata or a manual title is required.", nameof(request));
        }

        return new BookMetadataCandidate(
            "Manual",
            $"purchase:{request.PurchaseRequestId:N}",
            request.ManualTitle.Trim(),
            null,
            string.IsNullOrWhiteSpace(request.ManualAuthor) ? [] : [request.ManualAuthor.Trim()],
            null,
            request.PublicationYear?.ToString(),
            null,
            null,
            Isbn10: null,
            Isbn13: request.Isbn,
            ThumbnailUrl: null,
            InfoUrl: null);
    }

    private static BookCopyDuplicateStatus ResolveDuplicateStatus(
        ScanPurchaseRequest request,
        DuplicateDetectionResult duplicateDetection)
    {
        if (!duplicateDetection.HasPotentialDuplicate)
        {
            return BookCopyDuplicateStatus.ConfirmedUnique;
        }

        if (request.DuplicateStatus != BookCopyDuplicateStatus.ConfirmedDuplicate
            || request.DuplicateResolution == DuplicateResolution.NotSpecified)
        {
            throw new DuplicateConfirmationRequiredException(duplicateDetection);
        }

        return BookCopyDuplicateStatus.ConfirmedDuplicate;
    }

    private static void ValidateResolutionShape(ScanPurchaseRequest request)
    {
        if (!Enum.IsDefined(request.DuplicateResolution))
        {
            throw new ArgumentException("Duplicate resolution is invalid.", nameof(request));
        }

        if (!Enum.IsDefined(request.DuplicateStatus))
        {
            throw new ArgumentException("Duplicate status is invalid.", nameof(request));
        }

        switch (request.DuplicateResolution)
        {
            case DuplicateResolution.NotSpecified when request.ExistingBookEditionId.HasValue || request.ExistingBookWorkId.HasValue:
                throw new ArgumentException("Canonical ids require an explicit duplicate resolution.", nameof(request));
            case DuplicateResolution.ExistingEdition when !request.ExistingBookEditionId.HasValue:
                throw new ArgumentException("An existing book edition id is required for this resolution.", nameof(request));
            case DuplicateResolution.ExistingEdition when request.ExistingBookWorkId.HasValue:
                throw new ArgumentException("A book work id is not valid for an existing edition resolution.", nameof(request));
            case DuplicateResolution.SameWorkNewEdition when !request.ExistingBookWorkId.HasValue:
                throw new ArgumentException("An existing book work id is required for this resolution.", nameof(request));
            case DuplicateResolution.SameWorkNewEdition when request.ExistingBookEditionId.HasValue:
                throw new ArgumentException("An edition id is not valid for a same-work resolution.", nameof(request));
            case DuplicateResolution.NewWork when request.ExistingBookEditionId.HasValue || request.ExistingBookWorkId.HasValue:
                throw new ArgumentException("Existing canonical ids are not valid for a new-work resolution.", nameof(request));
        }
    }

    private static void ValidateSelectedResolution(
        ScanPurchaseRequest request,
        DuplicateDetectionResult duplicateDetection)
    {
        if (request.DuplicateResolution == DuplicateResolution.ExistingEdition
            && !duplicateDetection.Matches.Any(match => match.BookEditionId == request.ExistingBookEditionId))
        {
            throw new ArgumentException("The selected edition is not one of the detected duplicate matches.", nameof(request));
        }

        if (request.DuplicateResolution == DuplicateResolution.SameWorkNewEdition
            && !duplicateDetection.Matches.Any(match => match.BookWorkId == request.ExistingBookWorkId))
        {
            throw new ArgumentException("The selected work is not one of the detected duplicate matches.", nameof(request));
        }
    }

    private static async Task<Family?> LoadFamilyAsync(
        LibroryDbContext db,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        return await db.Families
            .AsSplitQuery()
            .Include(family => family.Members)
            .Include(family => family.BookCopies)
                .ThenInclude(copy => copy.BookEdition)
                    .ThenInclude(edition => edition.BookWork)
            .SingleOrDefaultAsync(family => family.Id == familyId, cancellationToken);
    }

    private static Task LoadWorkEditionsAsync(
        LibroryDbContext db,
        Guid bookWorkId,
        CancellationToken cancellationToken)
    {
        return db.BookEditions
            .Where(edition => edition.BookWorkId == bookWorkId)
            .LoadAsync(cancellationToken);
    }

    private static void ValidateRequest(ScanPurchaseRequest request)
    {
        if (request.ScanSessionId == Guid.Empty)
        {
            throw new ArgumentException("Scan session id is required.", nameof(request));
        }

        if (request.CandidateId == Guid.Empty)
        {
            throw new ArgumentException("Scan candidate id is required.", nameof(request));
        }

        if (request.PurchaseRequestId == Guid.Empty)
        {
            throw new ArgumentException("Purchase request id is required.", nameof(request));
        }

        if (request.OwnerMemberId == Guid.Empty)
        {
            throw new ArgumentException("Owner member id is required.", nameof(request));
        }
    }

    private static bool IsRetryablePostgresFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: "40001" or "40P01" })
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPurchaseRequestUniqueConstraint(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: "23505", ConstraintName: "IX_scan_candidates_PurchaseRequestId" })
            {
                return true;
            }
        }

        return false;
    }

    private static string? SelectIsbn(ScanPurchaseRequest request)
    {
        return Normalize(request.Isbn)
            ?? Normalize(request.SelectedMetadata?.Isbn13)
            ?? Normalize(request.SelectedMetadata?.Isbn10);
    }

    private static int? ParsePublicationYear(string? publishedDate)
    {
        if (string.IsNullOrWhiteSpace(publishedDate) || publishedDate.Trim().Length < 4)
        {
            return null;
        }

        return int.TryParse(publishedDate.Trim()[..4], out var year) && year is >= 1000 and <= 9999
            ? year
            : null;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
