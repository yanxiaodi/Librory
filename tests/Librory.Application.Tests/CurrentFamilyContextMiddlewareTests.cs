using System.Security.Claims;
using Librory.Application.Families;
using Librory.Domain.Models;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Librory.Application.Tests;

public class CurrentFamilyContextMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_populates_accessor_from_authenticated_user()
    {
        var familyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var accessor = new CurrentFamilyContextAccessor();
        var middleware = new CurrentFamilyContextMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext
        {
            User = BuildPrincipal(familyId, memberId, MemberRole.Member, PreferredLanguage.English),
        };

        await middleware.InvokeAsync(context, accessor);

        Assert.NotNull(accessor.Current);
        Assert.Equal(familyId, accessor.Current!.FamilyId);
        Assert.Equal(memberId, accessor.Current.MemberId);
        Assert.Equal(MemberRole.Member, accessor.Current.MemberRole);
        Assert.Equal(PreferredLanguage.English, accessor.Current.PreferredLanguage);
    }

    [Fact]
    public async Task InvokeAsync_clears_accessor_when_user_cannot_be_resolved()
    {
        var accessor = new CurrentFamilyContextAccessor
        {
            Current = new CurrentFamilyContext(
                Guid.NewGuid(),
                Guid.NewGuid(),
                MemberRole.Admin,
                PreferredLanguage.Chinese),
        };

        var middleware = new CurrentFamilyContextMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()),
        };

        await middleware.InvokeAsync(context, accessor);

        Assert.Null(accessor.Current);
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
