namespace Librory.Domain.Models;

public sealed class BookWork
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string CanonicalTitle { get; set; } = string.Empty;
    public string? CanonicalAuthor { get; set; }
    public LocalizedText? Summary { get; set; }
    public MetadataProvenance? SummaryProvenance { get; set; }
    public MetadataProvenance? CanonicalAuthorProvenance { get; set; }
    private readonly List<BookEdition> _editions = [];
    public IReadOnlyList<BookEdition> Editions => _editions;

    public static BookWork Create(string canonicalTitle, string? canonicalAuthor = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalTitle);

        return new BookWork
        {
            CanonicalTitle = canonicalTitle.Trim(),
            CanonicalAuthor = canonicalAuthor?.Trim(),
        };
    }

    public BookEdition AddEdition(
        string? isbn = null,
        string? format = null,
        int? publicationYear = null)
    {
        var edition = new BookEdition
        {
            Isbn = string.IsNullOrWhiteSpace(isbn) ? null : isbn.Trim(),
            Format = string.IsNullOrWhiteSpace(format) ? null : format.Trim(),
            PublicationYear = publicationYear,
        };

        edition.AssignToWork(this);
        return edition;
    }

    internal void RegisterEdition(BookEdition edition)
    {
        ArgumentNullException.ThrowIfNull(edition);

        if (_editions.All(existing => existing.Id != edition.Id))
        {
            _editions.Add(edition);
        }
    }
}
