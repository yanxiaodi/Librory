using Librory.Application.Recommendations;
using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public static class RecommendationProfileResponseFactory
{
    public static RecommendationProfileResponse Create(RecommendationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var dto = RecommendationProfileDtoFactory.Create(profile);
        return new RecommendationProfileResponse(
            dto.MemberId,
            dto.MinimumAge,
            dto.MaximumAge,
            dto.FavoriteAuthors,
            dto.FavoriteGenres,
            dto.FavoriteStyles);
    }
}
