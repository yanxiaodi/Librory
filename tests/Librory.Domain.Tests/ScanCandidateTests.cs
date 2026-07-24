using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public class ScanCandidateTests
{
    [Fact]
    public void ScanCandidate_create_trims_and_preserves_values()
    {
        var candidate = ScanCandidate.Create(
            "  Charlotte's Web  ",
            author: "  E. B. White  ",
            recommendationScore: 0.92m,
            isAlreadyOwned: true,
            duplicateMessage: "  Already owned by the family  ",
            confidenceLabel: "  High  ");

        Assert.NotEqual(Guid.Empty, candidate.Id);
        Assert.Equal("Charlotte's Web", candidate.DisplayTitle);
        Assert.Equal("E. B. White", candidate.Author);
        Assert.Equal(0.92m, candidate.RecommendationScore);
        Assert.True(candidate.IsAlreadyOwned);
        Assert.Equal("Already owned by the family", candidate.DuplicateMessage);
        Assert.Equal("High", candidate.ConfidenceLabel);
    }

    [Fact]
    public void ScanCandidate_create_rejects_blank_required_fields()
    {
        Assert.Throws<ArgumentException>(() => ScanCandidate.Create(" ", confidenceLabel: "High"));
        Assert.Throws<ArgumentException>(() => ScanCandidate.Create("Charlotte's Web", confidenceLabel: " "));
    }

    [Fact]
    public void ScanCandidate_create_rejects_out_of_range_recommendation_score()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ScanCandidate.Create("Charlotte's Web", recommendationScore: -0.01m, confidenceLabel: "Low"));
        Assert.Throws<ArgumentOutOfRangeException>(() => ScanCandidate.Create("Charlotte's Web", recommendationScore: 1.01m, confidenceLabel: "Low"));
    }
}
