using System.Security.Claims;
using Librory.Application.Families;
using Librory.Domain.Models;
using Xunit;

namespace Librory.Application.Tests;

public class CurrentFamilyContextResolverTests
{
    [Fact]
    public void TryResolve_returns_context_when_claims_are_valid()
    {
        var familyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var principal = BuildPrincipal(
            familyId,
            memberId,
            MemberRole.Admin,
            PreferredLanguage.Chinese);

        var resolved = CurrentFamilyContextResolver.TryResolve(principal, out var context);

        Assert.True(resolved);
        Assert.NotNull(context);
        Assert.Equal(familyId, context!.FamilyId);
        Assert.Equal(memberId, context.MemberId);
        Assert.Equal(MemberRole.Admin, context.MemberRole);
        Assert.Equal(PreferredLanguage.Chinese, context.PreferredLanguage);
    }

    [Fact]
    public void TryResolve_returns_false_when_claims_are_missing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var resolved = CurrentFamilyContextResolver.TryResolve(principal, out var context);

        Assert.False(resolved);
        Assert.Null(context);
    }

    [Fact]
    public void TryResolve_returns_false_when_claims_are_invalid()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(CurrentFamilyContextClaimTypes.FamilyId, "not-a-guid"),
            new Claim(CurrentFamilyContextClaimTypes.MemberId, Guid.NewGuid().ToString()),
            new Claim(CurrentFamilyContextClaimTypes.MemberRole, MemberRole.Admin.ToString()),
            new Claim(CurrentFamilyContextClaimTypes.PreferredLanguage, PreferredLanguage.English.ToString()),
        }, authenticationType: "test");

        var resolved = CurrentFamilyContextResolver.TryResolve(new ClaimsPrincipal(identity), out var context);

        Assert.False(resolved);
        Assert.Null(context);
    }

    private static ClaimsPrincipal BuildPrincipal(
        Guid familyId,
        Guid memberId,
        MemberRole memberRole,
        PreferredLanguage preferredLanguage)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(CurrentFamilyContextClaimTypes.FamilyId, familyId.ToString()),
            new Claim(CurrentFamilyContextClaimTypes.MemberId, memberId.ToString()),
            new Claim(CurrentFamilyContextClaimTypes.MemberRole, memberRole.ToString()),
            new Claim(CurrentFamilyContextClaimTypes.PreferredLanguage, preferredLanguage.ToString()),
        }, authenticationType: "test");

        return new ClaimsPrincipal(identity);
    }
}
