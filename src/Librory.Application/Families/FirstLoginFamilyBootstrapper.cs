using Librory.Domain.Models;

namespace Librory.Application.Families;

public static class FirstLoginFamilyBootstrapper
{
    public static FamilyBootstrapResult Bootstrap(
        string familyName,
        string displayName,
        ExternalIdentity externalIdentity,
        PreferredLanguage preferredLanguage = PreferredLanguage.English)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(externalIdentity);

        var family = Family.Create(familyName);
        var member = family.AddMember(displayName, MemberRole.Admin, preferredLanguage);
        // First-login bootstrap expects a brand-new identity; duplicate detection belongs in the
        // external identity lookup layer, not in this local composition helper.
        member.TryLinkExternalIdentity(externalIdentity);

        return new FamilyBootstrapResult(family, member);
    }
}
