using Librory.Application.Identity;
using Librory.Domain.Models;
using Xunit;

namespace Librory.Application.Tests;

public class ExternalIdentityResolverTests
{
    [Fact]
    public void Resolve_returns_null_when_no_match()
    {
        var accounts = new[] { UserAccount.Create() };

        var result = ExternalIdentityResolver.Resolve(
            accounts,
            ExternalIdentityProvider.Google,
            "not-found");

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_throws_when_accounts_is_null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExternalIdentityResolver.Resolve(
                null!,
                ExternalIdentityProvider.Google,
                "google-subject-123"));
    }

    [Fact]
    public void Resolve_throws_when_provider_subject_is_blank()
    {
        var accounts = new[] { UserAccount.Create() };

        Assert.Throws<ArgumentException>(() =>
            ExternalIdentityResolver.Resolve(
                accounts,
                ExternalIdentityProvider.Google,
                ""));
    }

    [Fact]
    public void Resolve_uses_provider_subject_and_ignores_email()
    {
        var account = UserAccount.Create("primary@example.com");
        account.TryLinkExternalIdentity(new ExternalIdentity(
            ExternalIdentityProvider.Google,
            "google-subject-123",
            Email: "primary@example.com"));

        var accounts = new[]
        {
            account,
            UserAccount.Create(),
        };

        var resolved = ExternalIdentityResolver.Resolve(
            accounts,
            ExternalIdentityProvider.Google,
            "google-subject-123");

        Assert.Same(account, resolved);
    }

    [Fact]
    public void Account_can_store_multiple_external_identities_but_reject_duplicates()
    {
        var account = UserAccount.Create("person@example.com");

        var googleLinked = account.TryLinkExternalIdentity(new ExternalIdentity(
            ExternalIdentityProvider.Google,
            "google-subject-123",
            Email: "person@example.com"));

        var microsoftLinked = account.TryLinkExternalIdentity(new ExternalIdentity(
            ExternalIdentityProvider.Microsoft,
            "microsoft-subject-456",
            Email: "person@outlook.com"));

        var duplicateGoogleLink = account.TryLinkExternalIdentity(new ExternalIdentity(
            ExternalIdentityProvider.Google,
            "google-subject-123",
            Email: "another@example.com"));

        Assert.True(googleLinked);
        Assert.True(microsoftLinked);
        Assert.False(duplicateGoogleLink);
        Assert.Equal(2, account.ExternalIdentities.Count);
        Assert.True(account.HasExternalIdentity(ExternalIdentityProvider.Google, "google-subject-123"));
        Assert.True(account.HasExternalIdentity(ExternalIdentityProvider.Microsoft, "microsoft-subject-456"));
    }
}
