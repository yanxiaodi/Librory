namespace Librory.Domain.Models;

public sealed class BookEdition
{
    public Guid Id { get; private set; }
    public Guid BookWorkId { get; private set; }
    public string? Isbn { get; set; }
    public string? Format { get; set; }
    public LocalizedText? Subtitle { get; set; }
    public int? PublicationYear { get; set; }
    public MetadataProvenance? SubtitleProvenance { get; set; }
    public MetadataProvenance? PublicationYearProvenance { get; set; }
    public BookWork BookWork { get; private set; } = null!;
    public bool IsAttachedToWork => BookWorkId != Guid.Empty;

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
}
