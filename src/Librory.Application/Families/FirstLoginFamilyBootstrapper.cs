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

        var account = UserAccount.Create(externalIdentity.Email);
        account.TryLinkExternalIdentity(externalIdentity);

        var family = Family.Create(familyName);
        var member = family.AddMember(displayName, MemberRole.Admin, preferredLanguage);
        member.LinkAccount(account);

        return new FamilyBootstrapResult(family, member, account);
    }
}
