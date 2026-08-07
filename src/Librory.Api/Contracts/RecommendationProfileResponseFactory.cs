using Librory.Application.Recommendations;
using Librory.Domain.Models;

namespace Librory.Api.Contracts;

public static class RecommendationProfileResponseFactory
{
    public static RecommendationProfileResponse Create(RecommendationProfile profile, bool includePrivateNotes = true)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var dto = RecommendationProfileDtoFactory.Create(profile);
        return new RecommendationProfileResponse(
            dto.MemberId,
            dto.MinimumAge,
            dto.MaximumAge,
            dto.FavoriteAuthors,
            dto.ExcludedAuthors,
            dto.FavoriteGenres,
            dto.ExcludedGenres,
            dto.FavoriteStyles,
            dto.ExcludedStyles,
            dto.PreferredBookLanguages,
            includePrivateNotes ? dto.PreferenceNotes : null,
            dto.ProfileVisibility,
            dto.UseInFamilyRecommendations);
    }
}
