namespace Librory.Domain.Models;

public sealed class WishlistItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid FamilyId { get; set; }
    public Guid? BookWorkId { get; set; }
    public Guid? BookEditionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Family Family { get; set; } = null!;
    public BookWork? BookWork { get; set; }
    public BookEdition? BookEdition { get; set; }
}
