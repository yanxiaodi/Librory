namespace Librory.Domain.Models;

public sealed class UserAccount
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string? Email { get; private set; }
    public List<ExternalIdentity> ExternalIdentities { get; } = [];
    public List<Member> Memberships { get; } = [];

    public static UserAccount Create(string? email = null)
    {
        return new UserAccount
        {
            Email = NormalizeEmail(email),
        };
    }

    public void SetEmail(string? email)
    {
        Email = NormalizeEmail(email);
    }

    public bool TryLinkExternalIdentity(ExternalIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (ExternalIdentities.Any(existing =>
            existing.Provider == identity.Provider &&
            string.Equals(existing.ProviderSubject, identity.ProviderSubject, StringComparison.Ordinal)))
        {
            return false;
        }

        ExternalIdentities.Add(identity);
        return true;
    }

    public bool HasExternalIdentity(ExternalIdentityProvider provider, string providerSubject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerSubject);

        return ExternalIdentities.Any(identity =>
            identity.Provider == provider &&
            string.Equals(identity.ProviderSubject, providerSubject, StringComparison.Ordinal));
    }

    internal void RegisterMembership(Member member)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (Memberships.All(existing => existing.Id != member.Id))
        {
            Memberships.Add(member);
        }
    }

    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }
}
