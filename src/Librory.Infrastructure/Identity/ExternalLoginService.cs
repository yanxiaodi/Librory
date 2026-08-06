using Librory.Application.Families;
using Librory.Application.Identity;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Librory.Infrastructure.Identity;

public sealed class ExternalLoginService(LibroryDbContext db) : IExternalLoginService
{
    private readonly LibroryDbContext _db = db;

    public async Task<ExternalLoginResult> SignInAsync(ExternalLoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProviderSubject);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SuggestedFamilyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SuggestedMemberDisplayName);

        var account = await _db.UserAccounts
            .Include(x => x.ExternalIdentities)
            .Include(x => x.Memberships)
                .ThenInclude(x => x.Family)
            .SingleOrDefaultAsync(x => x.ExternalIdentities.Any(identity =>
                identity.Provider == request.Provider &&
                identity.ProviderSubject == request.ProviderSubject), cancellationToken);

        if (account is not null)
        {
            if (account.Email is null && !string.IsNullOrWhiteSpace(request.Email))
            {
                account.SetEmail(request.Email);
            }

            var member = account.Memberships
                .Where(x => x.IsActive)
                .OrderBy(x => x.FamilyId)
                .ThenBy(x => x.Id)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("External identity has no active family membership.");

            return new ExternalLoginResult(
                account.Id,
                member.FamilyId,
                member.Family.Name,
                member.Id,
                member.DisplayName,
                member.Role,
                member.PreferredLanguage,
                false);
        }

        var bootstrap = FirstLoginFamilyBootstrapper.Bootstrap(
            request.SuggestedFamilyName,
            request.SuggestedMemberDisplayName,
            new ExternalIdentity(
                request.Provider,
                request.ProviderSubject,
                request.Email,
                request.DisplayName,
                DateTimeOffset.UtcNow),
            request.PreferredLanguage);

        _db.UserAccounts.Add(bootstrap.Account
            ?? throw new InvalidOperationException("First-login bootstrap did not create an account."));
        _db.Families.Add(bootstrap.Family);
        await _db.SaveChangesAsync(cancellationToken);

        return new ExternalLoginResult(
            bootstrap.Account.Id,
            bootstrap.Family.Id,
            bootstrap.Family.Name,
            bootstrap.InitialMember.Id,
            bootstrap.InitialMember.DisplayName,
            bootstrap.InitialMember.Role,
            bootstrap.InitialMember.PreferredLanguage,
            true);
    }
}
