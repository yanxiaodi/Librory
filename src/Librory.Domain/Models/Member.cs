namespace Librory.Domain.Models;

public sealed class Member
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid FamilyId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public MemberRole Role { get; set; } = MemberRole.Member;
    public PreferredLanguage PreferredLanguage { get; set; } = PreferredLanguage.English;
    public List<ExternalIdentity> ExternalIdentities { get; } = [];
    public Family Family { get; set; } = null!;

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

    public void AssignToFamily(Family family)
    {
        ArgumentNullException.ThrowIfNull(family);

        if (FamilyId != Guid.Empty && FamilyId != family.Id)
        {
            throw new InvalidOperationException("Member already belongs to a different family.");
        }

        FamilyId = family.Id;
        Family = family;
        family.RegisterMember(this);
    }
}
