namespace Librory.Domain.Models;

public sealed class ScanCandidate
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ScanSessionId { get; private set; }
    public string DisplayTitle { get; private set; } = string.Empty;
    public string? Author { get; private set; }
    public decimal RecommendationScore { get; private set; }
    public bool IsAlreadyOwned { get; private set; }
    public string? DuplicateMessage { get; private set; }
    public string ConfidenceLabel { get; private set; } = string.Empty;
    public ScanSession ScanSession { get; private set; } = null!;

    public static ScanCandidate Create(
        string displayTitle,
        string confidenceLabel,
        string? author = null,
        decimal recommendationScore = 0m,
        bool isAlreadyOwned = false,
        string? duplicateMessage = null)
    {
        Validate(displayTitle, confidenceLabel, recommendationScore);

        return new ScanCandidate
        {
            DisplayTitle = displayTitle.Trim(),
            Author = Normalize(author),
            RecommendationScore = recommendationScore,
            IsAlreadyOwned = isAlreadyOwned,
            DuplicateMessage = Normalize(duplicateMessage),
            ConfidenceLabel = confidenceLabel.Trim(),
        };
    }

    /// <summary>
    /// Mutates this candidate in place with corrected recognition or review data.
    /// </summary>
    public void ApplyCorrection(
        string displayTitle,
        string confidenceLabel,
        string? author = null,
        decimal recommendationScore = 0m,
        bool isAlreadyOwned = false,
        string? duplicateMessage = null)
    {
        Validate(displayTitle, confidenceLabel, recommendationScore);

        DisplayTitle = displayTitle.Trim();
        Author = Normalize(author);
        RecommendationScore = recommendationScore;
        IsAlreadyOwned = isAlreadyOwned;
        DuplicateMessage = Normalize(duplicateMessage);
        ConfidenceLabel = confidenceLabel.Trim();
    }

    internal void AttachTo(ScanSession scanSession)
    {
        ArgumentNullException.ThrowIfNull(scanSession);

        if (ScanSessionId != Guid.Empty && ScanSessionId != scanSession.Id)
        {
            throw new InvalidOperationException("Scan candidate already belongs to a different scan session.");
        }

        ScanSession = scanSession;
        ScanSessionId = scanSession.Id;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void Validate(string displayTitle, string confidenceLabel, decimal recommendationScore)
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
    }
}
