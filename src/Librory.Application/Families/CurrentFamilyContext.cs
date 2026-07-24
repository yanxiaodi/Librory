using Librory.Domain.Models;

namespace Librory.Application.Families;

public sealed record CurrentFamilyContext(
    Guid FamilyId,
    Guid MemberId,
    MemberRole MemberRole,
    PreferredLanguage PreferredLanguage);
