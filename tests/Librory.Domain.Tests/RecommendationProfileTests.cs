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
            favoriteGenres: ["Fantasy", "fantasy", "  fantasy  "],
            favoriteStyles: ["Reflective", "reflective"]);

        Assert.Single(family.RecommendationProfiles);
        Assert.Same(profile, family.RecommendationProfiles[0]);
        Assert.Equal(member.Id, profile.MemberId);
        Assert.Equal(8, profile.MinimumAge);
        Assert.Equal(12, profile.MaximumAge);
        Assert.Equal(["Roald Dahl"], profile.FavoriteAuthors);
        Assert.Equal(["Fantasy"], profile.FavoriteGenres);
        Assert.Equal(["Reflective"], profile.FavoriteStyles);
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
    public void Family_get_or_create_recommendation_profile_keeps_existing_preferences_when_only_age_changes()
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

        var updated = family.GetOrCreateRecommendationProfile(member, minimumAge: 5);

        Assert.Same(profile, updated);
        Assert.Equal(5, updated.MinimumAge);
        Assert.Equal(12, updated.MaximumAge);
        Assert.Equal(["Roald Dahl"], updated.FavoriteAuthors);
        Assert.Equal(["Fantasy"], updated.FavoriteGenres);
        Assert.Equal(["Reflective"], updated.FavoriteStyles);
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
    public void Family_get_or_create_recommendation_profile_rejects_null_member()
    {
        var family = Family.Create("The Yans");

        Assert.Throws<ArgumentNullException>(() => family.GetOrCreateRecommendationProfile(null!));
    }

    [Fact]
    public void Recommendation_profile_create_rejects_null_member()
    {
        Assert.Throws<ArgumentNullException>(() => RecommendationProfile.Create(null!));
    }

    [Fact]
    public void Family_get_or_create_recommendation_profile_rejects_duplicate_profiles_for_the_same_member()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");
        family.RecommendationProfiles.Add(RecommendationProfile.Create(member));
        family.RecommendationProfiles.Add(RecommendationProfile.Create(member));

        var exception = Assert.Throws<InvalidOperationException>(() => family.GetOrCreateRecommendationProfile(member));

        Assert.Equal("Family contains multiple recommendation profiles for the same member.", exception.Message);
    }

    [Fact]
    public void Recommendation_category_catalog_exposes_defaults_for_selection()
    {
        Assert.Contains("Fantasy", RecommendationCategoryCatalog.DefaultGenres);
        Assert.Contains("Character-driven", RecommendationCategoryCatalog.DefaultStyles);
    }
}
