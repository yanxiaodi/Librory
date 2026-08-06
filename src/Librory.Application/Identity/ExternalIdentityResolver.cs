using Librory.Domain.Models;

namespace Librory.Application.Identity;

public static class ExternalIdentityResolver
{
    public static UserAccount? Resolve(
        IEnumerable<UserAccount> accounts,
        ExternalIdentityProvider provider,
        string providerSubject)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerSubject);

        return accounts.FirstOrDefault(account => account.HasExternalIdentity(provider, providerSubject));
    }
}
