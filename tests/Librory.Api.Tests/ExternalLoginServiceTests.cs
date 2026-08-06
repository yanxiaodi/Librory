using Librory.Application.Identity;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Librory.Api.Tests;

public sealed class ExternalLoginServiceTests
{
    [Fact]
    public async Task SignInAsync_bootstraps_a_family_on_first_login()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IExternalLoginService>();
        var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();

        var result = await service.SignInAsync(
            new ExternalLoginRequest(
                ExternalIdentityProvider.Google,
                "google-subject-123",
                "alice@example.com",
                "Alice",
                "Alice Family",
                "Alice",
                PreferredLanguage.English),
            CancellationToken.None);

        Assert.True(result.IsNewMember);
        Assert.Equal("Alice Family", result.FamilyName);
        Assert.Equal("Alice", result.MemberDisplayName);
        Assert.Equal(MemberRole.Admin, result.MemberRole);
        Assert.Equal(PreferredLanguage.English, result.PreferredLanguage);

        Assert.Equal(1, await db.Families.CountAsync());
        Assert.Equal(1, await db.Members.CountAsync());

        var account = await db.UserAccounts
            .Include(x => x.ExternalIdentities)
            .SingleAsync();

        Assert.Equal("alice@example.com", account.Email);
        Assert.Single(account.ExternalIdentities);
        Assert.True(account.HasExternalIdentity(ExternalIdentityProvider.Google, "google-subject-123"));
    }

    [Fact]
    public async Task SignInAsync_backfills_email_for_a_migrated_account_and_selects_membership_deterministically()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IExternalLoginService>();
        var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();

        var account = UserAccount.Create();
        account.TryLinkExternalIdentity(new ExternalIdentity(
            ExternalIdentityProvider.Google,
            "google-subject-123",
            null,
            "Alice",
            DateTimeOffset.UtcNow));
        var firstFamily = new Family { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "First Family" };
        var firstMember = firstFamily.AddMember("Alice first");
        firstMember.LinkAccount(account);
        var secondFamily = new Family { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "Second Family" };
        var secondMember = secondFamily.AddMember("Alice second");
        secondMember.LinkAccount(account);
        db.UserAccounts.Add(account);
        db.Families.AddRange(firstFamily, secondFamily);
        await db.SaveChangesAsync();

        var result = await service.SignInAsync(
            new ExternalLoginRequest(
                ExternalIdentityProvider.Google,
                "google-subject-123",
                "alice@example.com",
                "Alice",
                "Alice Family",
                "Alice",
                PreferredLanguage.English),
            CancellationToken.None);

        Assert.Equal(firstFamily.Id, result.FamilyId);
        Assert.Equal("alice@example.com", (await db.UserAccounts.SingleAsync()).Email);
    }

    [Fact]
    public async Task SignInAsync_reuses_an_existing_member_for_the_same_provider_subject()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IExternalLoginService>();
        var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();

        var first = await service.SignInAsync(
            new ExternalLoginRequest(
                ExternalIdentityProvider.Google,
                "google-subject-123",
                "alice@example.com",
                "Alice",
                "Alice Family",
                "Alice",
                PreferredLanguage.English),
            CancellationToken.None);

        var second = await service.SignInAsync(
            new ExternalLoginRequest(
                ExternalIdentityProvider.Google,
                "google-subject-123",
                "alice@example.com",
                "Alice",
                "Alice Family",
                "Alice",
                PreferredLanguage.English),
            CancellationToken.None);

        Assert.False(second.IsNewMember);
        Assert.Equal(first.FamilyId, second.FamilyId);
        Assert.Equal(first.MemberId, second.MemberId);
        Assert.Equal(1, await db.Families.CountAsync());
        Assert.Equal(1, await db.Members.CountAsync());
    }
}
