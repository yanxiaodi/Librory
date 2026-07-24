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
        string? intakeNotes = null)
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
            intakeNotes);

        BookCopies.Add(copy);
        return copy;
    }

    public DuplicateDetectionResult DetectPotentialDuplicate(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var normalizedTitle = DuplicateDetectionResult.NormalizeTitle(title);
        var matches = new List<DuplicateMatch>();

        foreach (var copy in BookCopies)
        {
            var bookWork = RequireBookWork(copy);
            if (bookWork.NormalizedCanonicalTitle == normalizedTitle)
            {
                matches.Add(new DuplicateMatch(
                    copy.Id,
                    copy.BookEditionId,
                    copy.BookEdition.BookWorkId,
                    bookWork.CanonicalTitle,
                    copy.BookEdition.Isbn,
                    copy.BookEdition.Format,
                    copy.BookEdition.PublicationYear));
            }
        }

        return new DuplicateDetectionResult(title.Trim(), normalizedTitle, matches);
    }

    public DuplicateDetectionResult DetectPotentialDuplicate(BookEdition edition)
    {
        ArgumentNullException.ThrowIfNull(edition);

        if (edition.BookWorkId == Guid.Empty)
        {
            throw new InvalidOperationException("Edition must belong to a work before duplicate detection can run.");
        }

        return DetectPotentialDuplicate(edition.BookWork.CanonicalTitle);
    }

    private static BookWork RequireBookWork(BookCopy copy)
    {
        ArgumentNullException.ThrowIfNull(copy);
        ArgumentNullException.ThrowIfNull(copy.BookEdition);

        if (copy.BookEdition.BookWorkId == Guid.Empty || copy.BookEdition.BookWork is null)
        {
            throw new InvalidOperationException("Every copy must point to an edition that belongs to a work before duplicate detection can run.");
        }

        return copy.BookEdition.BookWork;
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

    public ScanSession StartScanSession(TimeSpan? retentionWindow = null)
    {
        var session = ScanSession.Create(this, retentionWindow);
        ScanSessions.Add(session);
        return session;
    }

    public RecommendationProfile GetOrCreateRecommendationProfile(
        Member member,
        int? minimumAge = null,
        int? maximumAge = null,
        IEnumerable<string>? favoriteAuthors = null,
        IEnumerable<string>? favoriteGenres = null,
        IEnumerable<string>? favoriteStyles = null)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (member.FamilyId != Id)
        {
            throw new InvalidOperationException("Member must belong to the same family as the recommendation profile.");
        }

        var profile = RecommendationProfiles.SingleOrDefault(existing => existing.MemberId == member.Id);
        if (profile is null)
        {
            profile = RecommendationProfile.Create(
                member,
                minimumAge,
                maximumAge,
                favoriteAuthors,
                favoriteGenres,
                favoriteStyles);
            RecommendationProfiles.Add(profile);
            return profile;
        }

        profile.UpdatePreferences(minimumAge, maximumAge, favoriteAuthors, favoriteGenres, favoriteStyles);
        return profile;
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
