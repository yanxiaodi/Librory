namespace Librory.Domain.Models;

public sealed class ScanCandidate
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ScanSessionId { get; private set; }
    public string DisplayTitle { get; private set; } = string.Empty;
    public string? Author { get; private set; }
    public int RecognitionRank { get; private set; }
    public decimal? RecommendationScore { get; private set; }
    public bool IsAlreadyOwned { get; private set; }
    public string? DuplicateMessage { get; private set; }
    public string ConfidenceLabel { get; private set; } = string.Empty;
    public PreferredLanguage? DetectedLanguage { get; private set; }
    public string? MetadataMatchesJson { get; private set; }
    public PurchaseStatus PurchaseStatus { get; private set; } = PurchaseStatus.Pending;
    public Guid? PurchasedBookCopyId { get; private set; }
    public Guid? PurchaseRequestId { get; private set; }
    public DateTimeOffset? PurchasedAt { get; private set; }
    public ScanSession ScanSession { get; private set; } = null!;

    public static ScanCandidate Create(
        string displayTitle,
        string confidenceLabel,
        string? author = null,
        decimal? recommendationScore = null,
        bool isAlreadyOwned = false,
        string? duplicateMessage = null,
        PreferredLanguage? detectedLanguage = null,
        int recognitionRank = 0)
    {
        Validate(displayTitle, confidenceLabel, recommendationScore, recognitionRank);

        return new ScanCandidate
        {
            DisplayTitle = displayTitle.Trim(),
            Author = Normalize(author),
            RecognitionRank = recognitionRank,
            RecommendationScore = recommendationScore,
            IsAlreadyOwned = isAlreadyOwned,
            DuplicateMessage = Normalize(duplicateMessage),
            ConfidenceLabel = confidenceLabel.Trim(),
            DetectedLanguage = detectedLanguage,
        };
    }

    /// <summary>
    /// Mutates this candidate in place with corrected recognition or review data.
    /// </summary>
    public void ApplyCorrection(
        string displayTitle,
        string confidenceLabel,
        string? author = null,
        decimal? recommendationScore = null,
        bool isAlreadyOwned = false,
        string? duplicateMessage = null,
        PreferredLanguage? detectedLanguage = null,
        int? recognitionRank = null)
    {
        EnsurePending();
        Validate(displayTitle, confidenceLabel, recommendationScore, recognitionRank);

        DisplayTitle = displayTitle.Trim();
        Author = Normalize(author);
        if (recognitionRank.HasValue)
        {
            RecognitionRank = recognitionRank.Value;
        }
        RecommendationScore = recommendationScore;
        IsAlreadyOwned = isAlreadyOwned;
        DuplicateMessage = Normalize(duplicateMessage);
        ConfidenceLabel = confidenceLabel.Trim();
        if (detectedLanguage.HasValue)
        {
            DetectedLanguage = detectedLanguage;
        }
    }

    public void ReplaceMetadataMatches(string? metadataMatchesJson, bool resetReviewState = true)
    {
        EnsurePending();

        MetadataMatchesJson = string.IsNullOrWhiteSpace(metadataMatchesJson)
            ? null
            : metadataMatchesJson.Trim();
        if (resetReviewState)
        {
            IsAlreadyOwned = false;
            DuplicateMessage = null;
        }
    }

    public void MarkPurchased(Guid bookCopyId, Guid purchaseRequestId, DateTimeOffset purchasedAt)
    {
        EnsurePending();

        if (bookCopyId == Guid.Empty)
        {
            throw new ArgumentException("Purchased book copy id is required.", nameof(bookCopyId));
        }

        if (purchaseRequestId == Guid.Empty)
        {
            throw new ArgumentException("Purchase request id is required.", nameof(purchaseRequestId));
        }

        PurchaseStatus = PurchaseStatus.Purchased;
        PurchasedBookCopyId = bookCopyId;
        PurchaseRequestId = purchaseRequestId;
        PurchasedAt = purchasedAt;
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

    private void EnsurePending()
    {
        if (PurchaseStatus == PurchaseStatus.Purchased)
        {
            throw new InvalidOperationException("A purchased scan candidate is read-only.");
        }
    }

    private static void Validate(string displayTitle, string confidenceLabel, decimal? recommendationScore, int? recognitionRank)
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

        if (recognitionRank < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(recognitionRank), recognitionRank, "Recognition rank cannot be negative.");
        }
    }
}
