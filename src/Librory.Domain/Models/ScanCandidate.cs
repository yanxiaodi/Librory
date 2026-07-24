namespace Librory.Domain.Models;

public sealed class ScanCandidate
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string DisplayTitle { get; set; } = string.Empty;
    public string? Author { get; set; }
    public decimal RecommendationScore { get; set; }
    public bool IsAlreadyOwned { get; set; }
    public string? DuplicateMessage { get; set; }
    public string ConfidenceLabel { get; set; } = string.Empty;
}
