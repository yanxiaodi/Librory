using Librory.Application.Identity;
using Librory.Domain.Models;
using Xunit;

namespace Librory.Application.Tests;

public class ExternalIdentityResolverTests
{
    [Fact]
    public void Resolve_returns_null_when_no_match()
    {
        var members = new[] { new Member() };

        var result = ExternalIdentityResolver.Resolve(
            members,
            ExternalIdentityProvider.Google,
            "not-found");

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_throws_when_members_is_null()
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
        var members = new[] { new Member() };

        Assert.Throws<ArgumentException>(() =>
            ExternalIdentityResolver.Resolve(
                members,
                ExternalIdentityProvider.Google,
                ""));
    }

    [Fact]
    public void Resolve_uses_provider_subject_and_ignores_email()
    {
        var member = new Member();
        member.TryLinkExternalIdentity(new ExternalIdentity(
            ExternalIdentityProvider.Google,
            "google-subject-123",
            Email: "primary@example.com"));

        var members = new[]
        {
            member,
            new Member()
        };

        var resolved = ExternalIdentityResolver.Resolve(
            members,
            ExternalIdentityProvider.Google,
            "google-subject-123");

        Assert.Same(member, resolved);
    }

    [Fact]
    public void Member_can_store_multiple_external_identities_but_reject_duplicates()
    {
        var member = new Member();

        var googleLinked = member.TryLinkExternalIdentity(new ExternalIdentity(
            ExternalIdentityProvider.Google,
            "google-subject-123",
            Email: "person@example.com"));

        var microsoftLinked = member.TryLinkExternalIdentity(new ExternalIdentity(
            ExternalIdentityProvider.Microsoft,
            "microsoft-subject-456",
            Email: "person@outlook.com"));

        var duplicateGoogleLink = member.TryLinkExternalIdentity(new ExternalIdentity(
            ExternalIdentityProvider.Google,
            "google-subject-123",
            Email: "another@example.com"));

        Assert.True(googleLinked);
        Assert.True(microsoftLinked);
        Assert.False(duplicateGoogleLink);
        Assert.Equal(2, member.ExternalIdentities.Count);
        Assert.True(member.HasExternalIdentity(ExternalIdentityProvider.Google, "google-subject-123"));
        Assert.True(member.HasExternalIdentity(ExternalIdentityProvider.Microsoft, "microsoft-subject-456"));
    }
}
