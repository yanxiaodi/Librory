namespace Librory.Domain.Models;

public sealed class ScanSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid FamilyId { get; private set; }
    public Family Family { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    private readonly List<ScanCandidate> _candidates = [];
    public IReadOnlyList<ScanCandidate> Candidates => _candidates;

    public static ScanSession Create(Family family, TimeSpan? retentionWindow = null)
    {
        ArgumentNullException.ThrowIfNull(family);

        var window = retentionWindow ?? TimeSpan.FromDays(7);
        if (window <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionWindow), window, "Retention window must be positive.");
        }

        var now = DateTimeOffset.UtcNow;
        var session = new ScanSession();
        session.AttachTo(family, now, window);
        return session;
    }

    private void AttachTo(Family family, DateTimeOffset createdAt, TimeSpan retentionWindow)
    {
        Family = family;
        FamilyId = family.Id;
        CreatedAt = createdAt;
        ExpiresAt = createdAt.Add(retentionWindow);
    }

    public void AddCandidate(ScanCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        _candidates.Add(candidate);
    }

    public bool IsExpired(DateTimeOffset? asOf = null)
    {
        return (asOf ?? DateTimeOffset.UtcNow) >= ExpiresAt;
    }
}
