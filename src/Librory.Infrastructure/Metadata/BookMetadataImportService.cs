using System.Data;
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
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (string.IsNullOrWhiteSpace(candidate.Title))
        {
            throw new ArgumentException("Title is required.", nameof(candidate));
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var isbn = SelectPreferredIsbn(candidate);
        if (!string.IsNullOrWhiteSpace(isbn))
        {
            var existingEdition = await _db.BookEditions
                .Include(x => x.BookWork)
                .ThenInclude(x => x.Editions)
                .FirstOrDefaultAsync(x => x.Isbn == isbn, cancellationToken);

            if (existingEdition is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return new BookMetadataImportResult(existingEdition.BookWork, false);
            }
        }

        var canonicalAuthor = NormalizeAuthors(candidate.Authors);
        var work = BookWork.Create(candidate.Title.Trim(), canonicalAuthor);
        if (!string.IsNullOrWhiteSpace(candidate.Description))
        {
            var description = candidate.Description.Trim();
            work.Summary = new LocalizedText(description);
            work.SummaryProvenance = CreateProvenance(candidate);
        }

        if (canonicalAuthor is not null)
        {
            work.CanonicalAuthorProvenance = CreateProvenance(candidate);
        }

        var publicationYear = ParsePublicationYear(candidate.PublishedDate);
        if (!string.IsNullOrWhiteSpace(isbn) || !string.IsNullOrWhiteSpace(candidate.Subtitle) || publicationYear.HasValue)
        {
            var edition = work.AddEdition(isbn, null, publicationYear);

            if (!string.IsNullOrWhiteSpace(candidate.Subtitle))
            {
                edition.Subtitle = new LocalizedText(candidate.Subtitle.Trim());
                edition.SubtitleProvenance = CreateProvenance(candidate);
            }

            if (publicationYear.HasValue)
            {
                edition.PublicationYearProvenance = CreateProvenance(candidate);
            }
        }

        _db.BookWorks.Add(work);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new BookMetadataImportResult(work, true);
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
        if (!string.IsNullOrWhiteSpace(candidate.Isbn13))
        {
            return candidate.Isbn13.Trim();
        }

        return string.IsNullOrWhiteSpace(candidate.Isbn10)
            ? null
            : candidate.Isbn10.Trim();
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

    private static MetadataProvenance CreateProvenance(BookMetadataCandidate candidate)
    {
        return new MetadataProvenance(
            candidate.Source.Trim(),
            candidate.SourceId.Trim(),
            1m,
            DateTimeOffset.UtcNow);
    }
}
