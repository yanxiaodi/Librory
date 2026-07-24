using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public class RecommendationProfileTests
{
    [Fact]
    public void Family_get_or_create_recommendation_profile_creates_one_profile_per_member()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");

        var profile = family.GetOrCreateRecommendationProfile(
            member,
            minimumAge: 8,
            maximumAge: 12,
            favoriteAuthors: ["Roald Dahl", "  Roald Dahl  ", " "],
            favoriteGenres: RecommendationCategoryCatalog.DefaultGenres.Take(2),
            favoriteStyles: RecommendationCategoryCatalog.DefaultStyles.Take(2));

        Assert.Single(family.RecommendationProfiles);
        Assert.Same(profile, family.RecommendationProfiles[0]);
        Assert.Equal(member.Id, profile.MemberId);
        Assert.Equal(8, profile.MinimumAge);
        Assert.Equal(12, profile.MaximumAge);
        Assert.Equal(["Roald Dahl"], profile.FavoriteAuthors);
        Assert.Equal(RecommendationCategoryCatalog.DefaultGenres.Take(2), profile.FavoriteGenres);
        Assert.Equal(RecommendationCategoryCatalog.DefaultStyles.Take(2), profile.FavoriteStyles);
    }

    [Fact]
    public void Family_get_or_create_recommendation_profile_updates_existing_profile_for_same_member()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");

        var first = family.GetOrCreateRecommendationProfile(member, minimumAge: 5, favoriteGenres: ["Fantasy"]);
        var second = family.GetOrCreateRecommendationProfile(
            member,
            minimumAge: 7,
            maximumAge: 11,
            favoriteAuthors: ["E. B. White"],
            favoriteGenres: ["Adventure", "Adventure"],
            favoriteStyles: ["Reflective"]);

        Assert.Same(first, second);
        Assert.Single(family.RecommendationProfiles);
        Assert.Equal(7, second.MinimumAge);
        Assert.Equal(11, second.MaximumAge);
        Assert.Equal(["E. B. White"], second.FavoriteAuthors);
        Assert.Equal(["Adventure"], second.FavoriteGenres);
        Assert.Equal(["Reflective"], second.FavoriteStyles);
    }

    [Fact]
    public void Family_get_or_create_recommendation_profile_rejects_member_from_other_family()
    {
        var firstFamily = Family.Create("First");
        var secondFamily = Family.Create("Second");
        var member = firstFamily.AddMember("Alice");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            secondFamily.GetOrCreateRecommendationProfile(member));

        Assert.Equal("Member must belong to the same family as the recommendation profile.", exception.Message);
    }

    [Fact]
    public void Recommendation_profile_rejects_invalid_age_range()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");

        Assert.Throws<InvalidOperationException>(() =>
            family.GetOrCreateRecommendationProfile(member, minimumAge: 12, maximumAge: 8));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            family.GetOrCreateRecommendationProfile(member, minimumAge: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            family.GetOrCreateRecommendationProfile(member, maximumAge: -1));
    }

    [Fact]
    public void Recommendation_category_catalog_exposes_defaults_for_selection()
    {
        Assert.Contains("Fantasy", RecommendationCategoryCatalog.DefaultGenres);
        Assert.Contains("Character-driven", RecommendationCategoryCatalog.DefaultStyles);
    }
}
