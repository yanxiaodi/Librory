namespace Librory.Domain.Models;

public sealed class BookCopy
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid FamilyId { get; set; }
    public Guid MemberId { get; set; }
    public Guid BookEditionId { get; set; }
    public string? Condition { get; set; }
    public string? PurchaseStore { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? ShelfLocation { get; set; }
    public DateTimeOffset? PurchasedAt { get; set; }
    public Family Family { get; set; } = null!;
    public Member Member { get; set; } = null!;
    public BookEdition BookEdition { get; set; } = null!;
}
