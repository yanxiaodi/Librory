using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public sealed record CurrentFamilyResponse(
    Guid FamilyId,
    string FamilyName,
    Guid MemberId,
    string MemberDisplayName,
    MemberRole MemberRole,
    PreferredLanguage PreferredLanguage,
    int MemberCount,
    int BookCount,
    int WishlistCount);
