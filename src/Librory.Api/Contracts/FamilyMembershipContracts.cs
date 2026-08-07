using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public sealed record FamilySummaryResponse(Guid FamilyId, string FamilyName, Guid MemberId, string MemberDisplayName, MemberRole Role, bool IsActive);

public sealed record CreateFamilyRequest(string FamilyName, string MemberDisplayName, PreferredLanguage PreferredLanguage = PreferredLanguage.English);

public sealed record CreateMemberRequest(string DisplayName, PreferredLanguage PreferredLanguage = PreferredLanguage.English);

public sealed record UpdateMemberRequest(string? DisplayName, PreferredLanguage? PreferredLanguage, MemberRole? Role);

public sealed record FamilyMemberResponse(
    Guid MemberId,
    string DisplayName,
    MemberRole Role,
    PreferredLanguage PreferredLanguage,
    bool IsActive,
    bool HasAccount,
    bool HasRecommendationProfile = false,
    ProfileVisibility? RecommendationProfileVisibility = null,
    bool CanUseForFamilyRecommendations = false);
