using Librory.Application.Recommendations;
using Librory.Domain.Models;
using Xunit;

namespace Librory.Application.Tests;

public class RecommendationProfileDtoTests
{
    [Fact]
    public void Recommendation_profile_dto_factory_projects_domain_profile()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");
        var profile = family.GetOrCreateRecommendationProfile(
            member,
            minimumAge: 8,
            maximumAge: 12,
            favoriteAuthors: ["Roald Dahl"],
            favoriteGenres: ["Fantasy"],
            favoriteStyles: ["Reflective"]);

        var dto = RecommendationProfileDtoFactory.Create(profile);

        Assert.Equal(member.Id, dto.MemberId);
        Assert.Equal(8, dto.MinimumAge);
        Assert.Equal(12, dto.MaximumAge);
        Assert.Equal(["Roald Dahl"], dto.FavoriteAuthors);
        Assert.Equal(["Fantasy"], dto.FavoriteGenres);
        Assert.Equal(["Reflective"], dto.FavoriteStyles);
    }
}
