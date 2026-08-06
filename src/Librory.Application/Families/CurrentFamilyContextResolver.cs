using System.Security.Claims;
using Librory.Domain.Models;

namespace Librory.Application.Families;

public static class CurrentFamilyContextResolver
{
    public static bool TryResolve(ClaimsPrincipal principal, out CurrentFamilyContext? context)
    {
        ArgumentNullException.ThrowIfNull(principal);

        context = null;

        if (principal.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var familyIdValue = principal.FindFirst(CurrentFamilyContextClaimTypes.FamilyId)?.Value;
        var memberIdValue = principal.FindFirst(CurrentFamilyContextClaimTypes.MemberId)?.Value;
        var accountIdValue = principal.FindFirst(CurrentFamilyContextClaimTypes.AccountId)?.Value;
        var roleValue = principal.FindFirst(CurrentFamilyContextClaimTypes.MemberRole)?.Value;
        var languageValue = principal.FindFirst(CurrentFamilyContextClaimTypes.PreferredLanguage)?.Value;

        if (!Guid.TryParse(familyIdValue, out var familyId) ||
            !Guid.TryParse(memberIdValue, out var memberId) ||
            !Enum.TryParse<MemberRole>(roleValue, ignoreCase: true, out var role) ||
            !Enum.TryParse<PreferredLanguage>(languageValue, ignoreCase: true, out var preferredLanguage))
        {
            return false;
        }

        Guid.TryParse(accountIdValue, out var accountId);
        context = new CurrentFamilyContext(familyId, memberId, role, preferredLanguage, accountId);
        return true;
    }
}
