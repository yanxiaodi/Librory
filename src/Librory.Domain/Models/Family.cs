namespace Librory.Domain.Models;

public sealed class Family
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public List<Member> Members { get; } = [];
    public List<BookCopy> BookCopies { get; } = [];
    public List<WishlistItem> WishlistItems { get; } = [];
    public List<RecommendationProfile> RecommendationProfiles { get; } = [];
    public List<ScanSession> ScanSessions { get; } = [];

    public static Family Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Family
        {
            Name = name.Trim(),
        };
    }

    public static Family CreateSharedFamily(
        string name,
        string adminDisplayName,
        PreferredLanguage preferredLanguage = PreferredLanguage.English)
    {
        var family = Create(name);
        family.AddMember(adminDisplayName, MemberRole.Admin, preferredLanguage);
        return family;
    }

    public BookCopy AddBookCopy(
        BookEdition edition,
        Member member,
        string? condition = null,
        string? purchaseStore = null,
        decimal? purchasePrice = null,
        string? shelfLocation = null,
        DateTimeOffset? purchasedAt = null,
        BookCopyDuplicateStatus duplicateStatus = BookCopyDuplicateStatus.Unchecked,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(edition);
        ArgumentNullException.ThrowIfNull(member);

        var copy = BookCopy.Create(
            edition,
            this,
            member,
            condition,
            purchaseStore,
            purchasePrice,
            shelfLocation,
            purchasedAt,
            duplicateStatus,
            notes);

        BookCopies.Add(copy);
        return copy;
    }

    public Member AddMember(
        string displayName,
        MemberRole role = MemberRole.Member,
        PreferredLanguage preferredLanguage = PreferredLanguage.English)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var member = new Member
        {
            DisplayName = displayName.Trim(),
            Role = role,
            PreferredLanguage = preferredLanguage,
        };

        member.AssignToFamily(this);
        return member;
    }

    internal void RegisterMember(Member member)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (Members.All(existing => existing.Id != member.Id))
        {
            Members.Add(member);
        }
    }
}
