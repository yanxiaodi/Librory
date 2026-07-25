using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public sealed record DevLoginResponse(
    Guid FamilyId,
    string FamilyName,
    Guid MemberId,
    string MemberDisplayName,
    MemberRole MemberRole,
    PreferredLanguage PreferredLanguage);
