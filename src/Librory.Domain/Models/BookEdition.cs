namespace Librory.Domain.Models;

public sealed class BookEdition
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid BookWorkId { get; set; }
    public string? Isbn { get; set; }
    public string? Format { get; set; }
    public int? PublicationYear { get; set; }
    public BookWork BookWork { get; set; } = null!;

    public void AssignToWork(BookWork work)
    {
        ArgumentNullException.ThrowIfNull(work);

        if (BookWorkId != Guid.Empty && BookWorkId != work.Id)
        {
            throw new InvalidOperationException("Edition already belongs to a different work.");
        }

        BookWorkId = work.Id;
        BookWork = work;
        work.RegisterEdition(this);
    }
}
