namespace Librory.Domain.Models;

public sealed class DuplicateDetectionResult
{
    private readonly List<DuplicateMatch> _matches;

    public DuplicateDetectionResult(string candidateTitle, string normalizedTitle, IReadOnlyList<DuplicateMatch> matches)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(candidateTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedTitle);
        ArgumentNullException.ThrowIfNull(matches);

        CandidateTitle = candidateTitle.Trim();
        NormalizedTitle = normalizedTitle.Trim();
        _matches = [.. matches];
    }

    public string CandidateTitle { get; }

    public string NormalizedTitle { get; }

    public IReadOnlyList<DuplicateMatch> Matches => _matches;

    public bool HasPotentialDuplicate => _matches.Count > 0;

    public string? FollowUpHint => HasPotentialDuplicate
        ? "Capture ISBN or barcode information to confirm the edition."
        : null;

    public static string NormalizeTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var builder = new System.Text.StringBuilder(title.Length);
        foreach (var character in title)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString();
    }
}

public sealed class DuplicateMatch
{
    public DuplicateMatch(
        Guid bookCopyId,
        Guid bookEditionId,
        Guid bookWorkId,
        string title,
        string? isbn,
        string? format,
        int? publicationYear)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        BookCopyId = bookCopyId;
        BookEditionId = bookEditionId;
        BookWorkId = bookWorkId;
        Title = title.Trim();
        Isbn = string.IsNullOrWhiteSpace(isbn) ? null : isbn.Trim();
        Format = string.IsNullOrWhiteSpace(format) ? null : format.Trim();
        PublicationYear = publicationYear;
    }

    public Guid BookCopyId { get; }

    public Guid BookEditionId { get; }

    public Guid BookWorkId { get; }

    public string Title { get; }

    public string? Isbn { get; }

    public string? Format { get; }

    public int? PublicationYear { get; }
}
