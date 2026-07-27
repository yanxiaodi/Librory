using System.Security.Claims;
using Librory.Application.Families;
using Librory.Application.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Librory.Api.Authentication;

public static class AuthenticationSessionFactory
{
    public static ClaimsPrincipal CreatePrincipal(ExternalLoginResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.MemberId.ToString()),
            new(ClaimTypes.Name, result.MemberDisplayName),
            new(CurrentFamilyContextClaimTypes.FamilyId, result.FamilyId.ToString()),
            new(CurrentFamilyContextClaimTypes.MemberId, result.MemberId.ToString()),
            new(CurrentFamilyContextClaimTypes.MemberRole, result.MemberRole.ToString()),
            new(CurrentFamilyContextClaimTypes.PreferredLanguage, result.PreferredLanguage.ToString()),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
