using Librory.Domain.Models;

namespace Librory.Application.Identity;

public sealed record ExternalLoginResult(
    Guid FamilyId,
    string FamilyName,
    Guid MemberId,
    string MemberDisplayName,
    MemberRole MemberRole,
    PreferredLanguage PreferredLanguage,
    bool IsNewMember);
