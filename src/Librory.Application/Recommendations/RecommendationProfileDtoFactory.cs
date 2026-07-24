using Librory.Domain.Models;

namespace Librory.Application.Recommendations;

public static class RecommendationProfileDtoFactory
{
    public static RecommendationProfileDto Create(RecommendationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new RecommendationProfileDto(
            profile.MemberId,
            profile.MinimumAge,
            profile.MaximumAge,
            profile.FavoriteAuthors.ToList(),
            profile.FavoriteGenres.ToList(),
            profile.FavoriteStyles.ToList());
    }
}
