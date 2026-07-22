namespace Librory.Domain.Models;

public sealed class Member
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid FamilyId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public MemberRole Role { get; set; } = MemberRole.Member;
    public PreferredLanguage PreferredLanguage { get; set; } = PreferredLanguage.English;
    public Family Family { get; set; } = null!;
    public RecommendationProfile? RecommendationProfile { get; set; }
}
