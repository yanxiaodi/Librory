using Librory.Domain.Models;

namespace Librory.Application.Identity;

public static class ExternalIdentityResolver
{
    public static Member? Resolve(
        IEnumerable<Member> members,
        ExternalIdentityProvider provider,
        string providerSubject)
    {
        ArgumentNullException.ThrowIfNull(members);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerSubject);

        return members.FirstOrDefault(member => member.HasExternalIdentity(provider, providerSubject));
    }
}
