namespace Librory.Domain.Models;

public sealed class ScanCandidate
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string DisplayTitle { get; private set; } = string.Empty;
    public string? Author { get; private set; }
    public decimal RecommendationScore { get; private set; }
    public bool IsAlreadyOwned { get; private set; }
    public string? DuplicateMessage { get; private set; }
    public string ConfidenceLabel { get; private set; } = string.Empty;

    public static ScanCandidate Create(
        string displayTitle,
        string? author = null,
        decimal recommendationScore = 0m,
        bool isAlreadyOwned = false,
        string? duplicateMessage = null,
        string confidenceLabel = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(confidenceLabel);

        if (recommendationScore is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recommendationScore),
                recommendationScore,
                "Recommendation score must be between 0 and 1.");
        }

        return new ScanCandidate
        {
            DisplayTitle = displayTitle.Trim(),
            Author = string.IsNullOrWhiteSpace(author) ? null : author.Trim(),
            RecommendationScore = recommendationScore,
            IsAlreadyOwned = isAlreadyOwned,
            DuplicateMessage = string.IsNullOrWhiteSpace(duplicateMessage) ? null : duplicateMessage.Trim(),
            ConfidenceLabel = confidenceLabel.Trim(),
        };
    }
}
