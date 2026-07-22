namespace Librory.Domain.Models;

public sealed class BookWork
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string CanonicalTitle { get; set; } = string.Empty;
    public string? CanonicalAuthor { get; set; }
    public LocalizedText? Summary { get; set; }
}
