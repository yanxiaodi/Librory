namespace Librory.Domain.Models;

public sealed class BookWork
{
    public Guid Id { get; init; } = Guid.NewGuid();
    private string _canonicalTitle = string.Empty;
    private string _normalizedCanonicalTitle = string.Empty;
    public string CanonicalTitle
    {
        get => _canonicalTitle.Trim();
        set => SetCanonicalTitle(value);
    }
    public string NormalizedCanonicalTitle => string.IsNullOrEmpty(_normalizedCanonicalTitle)
        ? DuplicateDetectionResult.NormalizeTitle(CanonicalTitle)
        : _normalizedCanonicalTitle;
    public string? CanonicalAuthor { get; set; }
    public LocalizedText? Summary { get; set; }
    public MetadataProvenance? SummaryProvenance { get; set; }
    public MetadataProvenance? CanonicalAuthorProvenance { get; set; }
    private readonly List<BookEdition> _editions = [];
    public IReadOnlyList<BookEdition> Editions => _editions;

    public static BookWork Create(string canonicalTitle, string? canonicalAuthor = null)
    {
        return new BookWork
        {
            CanonicalTitle = canonicalTitle,
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

    private void SetCanonicalTitle(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        _canonicalTitle = value.Trim();
        _normalizedCanonicalTitle = DuplicateDetectionResult.NormalizeTitle(_canonicalTitle);
    }
}
