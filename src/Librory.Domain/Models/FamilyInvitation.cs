namespace Librory.Domain.Models;

public sealed class FamilyInvitation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid FamilyId { get; private set; }
    public Guid? TargetMemberId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public FamilyInvitationStatus Status { get; private set; } = FamilyInvitationStatus.Pending;
    public Guid CreatedByMemberId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public Guid? AcceptedAccountId { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public Guid? RevokedByMemberId { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? SupersededByInvitationId { get; private set; }

    private FamilyInvitation()
    {
    }

    public static FamilyInvitation Create(
        Guid familyId,
        string email,
        string tokenHash,
        Guid createdByMemberId,
        DateTimeOffset expiresAt,
        Guid? targetMemberId = null,
        DateTimeOffset? createdAt = null)
    {
        if (familyId == Guid.Empty)
        {
            throw new ArgumentException("Family id is required.", nameof(familyId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (createdByMemberId == Guid.Empty)
        {
            throw new ArgumentException("Creating member id is required.", nameof(createdByMemberId));
        }

        var now = createdAt ?? DateTimeOffset.UtcNow;
        if (expiresAt <= now)
        {
            throw new ArgumentException("Invitation expiry must be in the future.", nameof(expiresAt));
        }

        return new FamilyInvitation
        {
            FamilyId = familyId,
            TargetMemberId = targetMemberId,
            Email = email.Trim().ToLowerInvariant(),
            TokenHash = tokenHash,
            CreatedByMemberId = createdByMemberId,
            CreatedAt = now,
            ExpiresAt = expiresAt,
        };
    }

    public void Accept(Guid accountId, DateTimeOffset acceptedAt)
    {
        EnsurePending(acceptedAt);

        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("Account id is required.", nameof(accountId));
        }

        Status = FamilyInvitationStatus.Accepted;
        AcceptedAccountId = accountId;
        AcceptedAt = acceptedAt;
    }

    public void Revoke(Guid revokedByMemberId, DateTimeOffset revokedAt)
    {
        EnsurePending(revokedAt);

        if (revokedByMemberId == Guid.Empty)
        {
            throw new ArgumentException("Revoking member id is required.", nameof(revokedByMemberId));
        }

        Status = FamilyInvitationStatus.Revoked;
        RevokedByMemberId = revokedByMemberId;
        RevokedAt = revokedAt;
    }

    public void Supersede(Guid supersedingInvitationId)
    {
        if (Status != FamilyInvitationStatus.Pending)
        {
            throw new InvalidOperationException("Only pending invitations can be superseded.");
        }

        if (supersedingInvitationId == Guid.Empty)
        {
            throw new ArgumentException("Superseding invitation id is required.", nameof(supersedingInvitationId));
        }

        Status = FamilyInvitationStatus.Superseded;
        SupersededByInvitationId = supersedingInvitationId;
    }

    public void Expire(DateTimeOffset now)
    {
        if (Status == FamilyInvitationStatus.Pending && ExpiresAt <= now)
        {
            Status = FamilyInvitationStatus.Expired;
        }
    }

    private void EnsurePending(DateTimeOffset now)
    {
        Expire(now);
        if (Status != FamilyInvitationStatus.Pending)
        {
            throw new InvalidOperationException("Only pending invitations can change state.");
        }
    }
}
