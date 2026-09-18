namespace Librory.Domain.Models;

public sealed class BookEdition
{
    public Guid Id { get; private set; }
    public Guid BookWorkId { get; private set; }
    public string? Isbn { get; set; }
    public string? Format { get; set; }
    public LocalizedText? Subtitle { get; set; }
    public int? PublicationYear { get; set; }
    public bool IsProvisional { get; set; }
    public MetadataProvenance? SubtitleProvenance { get; set; }
    public MetadataProvenance? PublicationYearProvenance { get; set; }
    public BookWork BookWork { get; private set; } = null!;
    public bool IsAttachedToWork => BookWorkId != Guid.Empty;

    public void ConfirmVersion()
    {
        if (string.IsNullOrWhiteSpace(Isbn) && string.IsNullOrWhiteSpace(Format) && !PublicationYear.HasValue)
        {
            throw new InvalidOperationException("At least one version field is required to confirm an edition.");
        }

        IsProvisional = false;
    }

    public void UpdateVersion(string? isbn, string? format, int? publicationYear)
    {
        Isbn = Normalize(isbn) ?? Isbn;
        Format = Normalize(format) ?? Format;
        if (publicationYear.HasValue)
        {
            PublicationYear = publicationYear;
            PublicationYearProvenance = new MetadataProvenance(
                "Manual",
                "publication-year",
                1m,
                DateTimeOffset.UtcNow);
        }

        ConfirmVersion();
    }

    public BookEdition()
    {
        Id = Guid.NewGuid();
    }

    public void AssignToWork(BookWork work)
    {
        ArgumentNullException.ThrowIfNull(work);

        if (IsAttachedToWork && BookWorkId != work.Id)
        {
            throw new InvalidOperationException("Edition already belongs to a different work.");
        }

        BookWorkId = work.Id;
        BookWork = work;
        work.RegisterEdition(this);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
