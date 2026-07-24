namespace Librory.Domain.Models;

public sealed class RecommendationProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid MemberId { get; private set; }
    public int? MinimumAge { get; private set; }
    public int? MaximumAge { get; private set; }
    public List<string> FavoriteAuthors { get; } = [];
    public List<string> FavoriteGenres { get; } = [];
    public List<string> FavoriteStyles { get; } = [];
    public Member Member { get; private set; } = null!;

    public void UpdatePreferences(
        int? minimumAge = null,
        int? maximumAge = null,
        IEnumerable<string>? favoriteAuthors = null,
        IEnumerable<string>? favoriteGenres = null,
        IEnumerable<string>? favoriteStyles = null)
    {
        var effectiveMinimumAge = minimumAge ?? MinimumAge;
        var effectiveMaximumAge = maximumAge ?? MaximumAge;

        ValidateAgeRange(effectiveMinimumAge, effectiveMaximumAge);

        MinimumAge = effectiveMinimumAge;
        MaximumAge = effectiveMaximumAge;

        UpdateValues(FavoriteAuthors, favoriteAuthors);
        UpdateValues(FavoriteGenres, favoriteGenres);
        UpdateValues(FavoriteStyles, favoriteStyles);
    }

    public static RecommendationProfile Create(
        Member member,
        int? minimumAge = null,
        int? maximumAge = null,
        IEnumerable<string>? favoriteAuthors = null,
        IEnumerable<string>? favoriteGenres = null,
        IEnumerable<string>? favoriteStyles = null)
    {
        ArgumentNullException.ThrowIfNull(member);

        var profile = new RecommendationProfile
        {
            Member = member,
            MemberId = member.Id,
        };

        profile.UpdatePreferences(minimumAge, maximumAge, favoriteAuthors, favoriteGenres, favoriteStyles);
        return profile;
    }

    private static void ValidateAgeRange(int? minimumAge, int? maximumAge)
    {
        if (minimumAge is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumAge), minimumAge, "Minimum age must be non-negative.");
        }

        if (maximumAge is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAge), maximumAge, "Maximum age must be non-negative.");
        }

        if (minimumAge.HasValue && maximumAge.HasValue && minimumAge > maximumAge)
        {
            throw new InvalidOperationException("Minimum age cannot be greater than maximum age.");
        }
    }

    private static void UpdateValues(List<string> target, IEnumerable<string>? values)
    {
        if (values is null)
        {
            return;
        }

        target.Clear();

        foreach (var value in NormalizeValues(values))
        {
            if (!target.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                target.Add(value);
            }
        }
    }

    private static IEnumerable<string> NormalizeValues(IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            yield return value.Trim();
        }
    }
}
