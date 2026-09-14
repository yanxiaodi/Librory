using Librory.Application.Metadata;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Librory.Infrastructure.Metadata;

public sealed class BookMetadataImportService : IBookMetadataImportService
{
    private readonly LibroryDbContext _db;

    public BookMetadataImportService(LibroryDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);

        _db = db;
    }

    public async Task<BookMetadataImportResult> ImportAsync(
        BookMetadataCandidate candidate,
        CancellationToken cancellationToken,
        BookMetadataImportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (string.IsNullOrWhiteSpace(candidate.Title))
        {
            throw new ArgumentException("Title is required.", nameof(candidate));
        }

        options ??= new BookMetadataImportOptions();
        var isbn = Normalize(options.Isbn) ?? SelectPreferredIsbn(candidate);
        if (options.ReuseExistingEditionByIsbn && !string.IsNullOrWhiteSpace(isbn))
        {
            var existingEdition = await _db.BookEditions
                .Include(x => x.BookWork)
                .ThenInclude(x => x.Editions)
                .FirstOrDefaultAsync(x => x.Isbn == isbn, cancellationToken);

            if (existingEdition is not null)
            {
                return new BookMetadataImportResult(existingEdition.BookWork, false, existingEdition);
            }
        }

        var canonicalAuthor = NormalizeAuthors(candidate.Authors);
        var work = options.TargetWork ?? BookWork.Create(candidate.Title.Trim(), canonicalAuthor);
        var provenanceCapturedAt = DateTimeOffset.UtcNow;
        if (options.TargetWork is null)
        {
            ApplyWorkMetadata(work, candidate, provenanceCapturedAt);
        }

        var publicationYear = options.PublicationYear ?? ParsePublicationYear(candidate.PublishedDate);
        var format = Normalize(options.Format);
        var shouldCreateEdition = options.AllowProvisionalEdition
            || !string.IsNullOrWhiteSpace(isbn)
            || !string.IsNullOrWhiteSpace(format)
            || !string.IsNullOrWhiteSpace(candidate.Subtitle)
            || publicationYear.HasValue;

        BookEdition? edition = null;
        if (shouldCreateEdition)
        {
            edition = work.AddEdition(isbn, format, publicationYear);
            edition.IsProvisional = options.AllowProvisionalEdition
                && string.IsNullOrWhiteSpace(isbn)
                && string.IsNullOrWhiteSpace(format)
                && !publicationYear.HasValue;
            ApplyEditionMetadata(edition, candidate, provenanceCapturedAt);
        }

        if (options.TargetWork is null)
        {
            _db.BookWorks.Add(work);
        }

        return new BookMetadataImportResult(work, options.TargetWork is null, edition);
    }

    private static void ApplyWorkMetadata(
        BookWork work,
        BookMetadataCandidate candidate,
        DateTimeOffset capturedAt)
    {
        if (!string.IsNullOrWhiteSpace(candidate.Description))
        {
            work.Summary = new LocalizedText(candidate.Description.Trim());
            work.SummaryProvenance = CreateProvenance(candidate, capturedAt);
        }

        if (!string.IsNullOrWhiteSpace(work.CanonicalAuthor))
        {
            work.CanonicalAuthorProvenance = CreateProvenance(candidate, capturedAt);
        }
    }

    private static void ApplyEditionMetadata(
        BookEdition edition,
        BookMetadataCandidate candidate,
        DateTimeOffset capturedAt)
    {
        if (!string.IsNullOrWhiteSpace(candidate.Subtitle))
        {
            edition.Subtitle = new LocalizedText(candidate.Subtitle.Trim());
            edition.SubtitleProvenance = CreateProvenance(candidate, capturedAt);
        }

        if (ParsePublicationYear(candidate.PublishedDate).HasValue)
        {
            edition.PublicationYearProvenance = CreateProvenance(candidate, capturedAt);
        }
    }

    private static string? NormalizeAuthors(IReadOnlyList<string>? authors)
    {
        var normalizedAuthors = (authors ?? [])
            .Where(author => !string.IsNullOrWhiteSpace(author))
            .Select(author => author.Trim())
            .ToArray();

        return normalizedAuthors.Length == 0
            ? null
            : string.Join(", ", normalizedAuthors);
    }

    private static string? SelectPreferredIsbn(BookMetadataCandidate candidate)
    {
        return Normalize(candidate.Isbn13) ?? Normalize(candidate.Isbn10);
    }

    private static int? ParsePublicationYear(string? publishedDate)
    {
        if (string.IsNullOrWhiteSpace(publishedDate))
        {
            return null;
        }

        var trimmed = publishedDate.Trim();
        if (trimmed.Length < 4)
        {
            return null;
        }

        var yearText = trimmed[..4];
        return int.TryParse(yearText, out var year) && year is >= 1000 and <= 9999
            ? year
            : null;
    }

    private static MetadataProvenance CreateProvenance(BookMetadataCandidate candidate, DateTimeOffset capturedAt)
    {
        return new MetadataProvenance(
            candidate.Source.Trim(),
            candidate.SourceId.Trim(),
            1m,
            capturedAt);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
