namespace Librory.Domain.Models;

public sealed class ScanSession
{
    private const int MaxShelfPhotoPathLength = 400;

    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid FamilyId { get; private set; }
    public string ShelfPhotoPath { get; private set; } = string.Empty;
    public Family Family { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public Guid? TargetMemberId { get; private set; }
    public bool TargetProfileAvailable { get; private set; }
    public bool TargetProfileUsed { get; private set; }
    public PreferredLanguage? InferredLanguage { get; private set; }
    public bool HasMixedLanguages { get; private set; }
    public ScanLanguageContext LanguageContext => new(InferredLanguage, HasMixedLanguages);
    private readonly List<ScanCandidate> _candidates = [];
    public IReadOnlyList<ScanCandidate> Candidates => _candidates;

    public static ScanSession Create(Family family, string shelfPhotoPath, TimeSpan? retentionWindow = null)
    {
        return Create(family, null, false, false, shelfPhotoPath, retentionWindow);
    }

    public static ScanSession Create(
        Family family,
        Guid? targetMemberId,
        bool targetProfileAvailable,
        bool targetProfileUsed,
        string shelfPhotoPath,
        TimeSpan? retentionWindow = null)
    {
        ArgumentNullException.ThrowIfNull(family);
        var window = retentionWindow ?? TimeSpan.FromDays(7);
        if (window <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionWindow), window, "Retention window must be positive.");
        }

        var now = DateTimeOffset.UtcNow;
        var session = new ScanSession();
        session.AttachTo(
            family,
            targetMemberId,
            targetProfileAvailable,
            targetProfileUsed,
            shelfPhotoPath,
            now,
            window);
        return session;
    }

    private void AttachTo(
        Family family,
        Guid? targetMemberId,
        bool targetProfileAvailable,
        bool targetProfileUsed,
        string shelfPhotoPath,
        DateTimeOffset createdAt,
        TimeSpan retentionWindow)
    {
        Family = family;
        FamilyId = family.Id;
        ShelfPhotoPath = NormalizeShelfPhotoPath(shelfPhotoPath);
        CreatedAt = createdAt;
        ExpiresAt = createdAt.Add(retentionWindow);
        TargetMemberId = targetMemberId;
        TargetProfileAvailable = targetProfileAvailable;
        TargetProfileUsed = targetProfileUsed;
    }

    public void AddCandidate(ScanCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        candidate.AttachTo(this);
        _candidates.Add(candidate);
        RecalculateLanguageContext();
    }

    public void RemoveCandidate(Guid candidateId)
    {
        var candidate = GetCandidateById(candidateId);
        _candidates.Remove(candidate);
    }

    public void CorrectCandidate(
        Guid candidateId,
        string displayTitle,
        string confidenceLabel,
        string? author = null,
        decimal recommendationScore = 0m,
        bool isAlreadyOwned = false,
        string? duplicateMessage = null,
        PreferredLanguage? detectedLanguage = null)
    {
        var candidate = GetCandidateById(candidateId);
        candidate.ApplyCorrection(
            displayTitle,
            confidenceLabel,
            author,
            recommendationScore,
            isAlreadyOwned,
            duplicateMessage,
            detectedLanguage);
        RecalculateLanguageContext();
    }

    public bool IsExpired(DateTimeOffset? asOf = null)
    {
        return (asOf ?? DateTimeOffset.UtcNow) >= ExpiresAt;
    }

    private ScanCandidate GetCandidateById(Guid candidateId)
    {
        var candidate = _candidates.SingleOrDefault(existing => existing.Id == candidateId);
        if (candidate is null)
        {
            throw new InvalidOperationException("Candidate not found in this scan session.");
        }

        return candidate;
    }

    private void RecalculateLanguageContext()
    {
        var knownLanguages = _candidates
            .Where(candidate => candidate.DetectedLanguage.HasValue)
            .Select(candidate => candidate.DetectedLanguage!.Value)
            .ToList();

        var counts = knownLanguages
            .GroupBy(language => language)
            .Select(group => (Language: group.Key, Count: group.Count()))
            .OrderByDescending(group => group.Count)
            .ToList();

        HasMixedLanguages = counts.Count > 1;
        InferredLanguage = counts.Count > 0 && counts[0].Count > knownLanguages.Count - counts[0].Count
            ? counts[0].Language
            : null;
    }

    private static string NormalizeShelfPhotoPath(string shelfPhotoPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shelfPhotoPath);

        var normalized = shelfPhotoPath.Trim();
        if (normalized.Length > MaxShelfPhotoPathLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(shelfPhotoPath),
                normalized,
                $"Shelf photo path must be {MaxShelfPhotoPathLength} characters or fewer.");
        }

        return normalized;
    }
}
