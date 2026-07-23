using Librory.Domain.Models;

namespace Librory.Application.Families;

public static class SharedFamilyCreator
{
    public static FamilyBootstrapResult Create(
        string familyName,
        string adminDisplayName,
        PreferredLanguage preferredLanguage = PreferredLanguage.English)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(adminDisplayName);

        var family = Family.CreateSharedFamily(familyName, adminDisplayName, preferredLanguage);
        var admin = family.Members.Single(member => member.Role == MemberRole.Admin);

        return new FamilyBootstrapResult(family, admin);
    }
}
