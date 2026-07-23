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

        // TODO(story-01b or later): replace this in-memory lookup with a repository query.
        return members.FirstOrDefault(member => member.HasExternalIdentity(provider, providerSubject));
    }
}
