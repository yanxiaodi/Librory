using Librory.Application.Families;
using Librory.Domain.Models;
using Xunit;

namespace Librory.Application.Tests;

public class FirstLoginFamilyBootstrapperTests
{
    [Fact]
    public void Bootstrap_creates_family_admin_and_links_external_identity()
    {
        var result = FirstLoginFamilyBootstrapper.Bootstrap(
            "  The Yans  ",
            "  Alice  ",
            new ExternalIdentity(
                ExternalIdentityProvider.Google,
                "google-subject-123",
                Email: "alice@example.com",
                DisplayName: "Alice"),
            PreferredLanguage.Chinese);

        Assert.Equal("The Yans", result.Family.Name);
        Assert.Single(result.Family.Members);
        Assert.Same(result.InitialMember, result.Family.Members[0]);
        Assert.Equal(MemberRole.Admin, result.InitialMember.Role);
        Assert.Equal("Alice", result.InitialMember.DisplayName);
        Assert.Equal(PreferredLanguage.Chinese, result.InitialMember.PreferredLanguage);
        Assert.True(result.InitialMember.HasExternalIdentity(
            ExternalIdentityProvider.Google,
            "google-subject-123"));
    }

    [Fact]
    public void Bootstrap_uses_default_preferred_language_when_not_specified()
    {
        var result = FirstLoginFamilyBootstrapper.Bootstrap(
            "The Yans",
            "Alice",
            new ExternalIdentity(
                ExternalIdentityProvider.Google,
                "google-subject-123"));

        Assert.Equal(PreferredLanguage.English, result.InitialMember.PreferredLanguage);
    }

    [Fact]
    public void Bootstrap_throws_when_family_name_is_blank()
    {
        Assert.Throws<ArgumentException>(() => FirstLoginFamilyBootstrapper.Bootstrap(
            " ",
            "Alice",
            new ExternalIdentity(ExternalIdentityProvider.Google, "google-subject-123")));
    }

    [Fact]
    public void Bootstrap_throws_when_display_name_is_blank()
    {
        Assert.Throws<ArgumentException>(() => FirstLoginFamilyBootstrapper.Bootstrap(
            "The Yans",
            " ",
            new ExternalIdentity(ExternalIdentityProvider.Google, "google-subject-123")));
    }

    [Fact]
    public void Bootstrap_throws_when_external_identity_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => FirstLoginFamilyBootstrapper.Bootstrap(
            "The Yans",
            "Alice",
            null!));
    }
}
