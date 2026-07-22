namespace Librory.Domain.Models;

public sealed class BookEdition
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid BookWorkId { get; set; }
    public string? Isbn { get; set; }
    public string? Format { get; set; }
    public int? PublicationYear { get; set; }
    public BookWork? BookWork { get; set; }
    public List<BookCopy> Copies { get; } = [];
}
