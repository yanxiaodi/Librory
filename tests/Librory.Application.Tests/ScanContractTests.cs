using Librory.Application.Scanning;
using Xunit;

namespace Librory.Application.Tests;

public class ScanContractTests
{
    [Fact]
    public void ScanShelfRequest_has_family_and_language_fields()
    {
        var request = new ScanShelfRequest(Guid.NewGuid(), "zh", "shelf-photo.jpg");

        Assert.Equal("zh", request.PreferredLanguage);
        Assert.Equal("shelf-photo.jpg", request.ShelfPhotoPath);
    }

    [Fact]
    public void ScanSessionDto_can_carry_candidates_and_expiration()
    {
        var candidate = new ScanCandidateDto(
            Guid.NewGuid(),
            "Charlotte's Web",
            "E. B. White",
            0.92m,
            true,
            "Already owned by the family",
            "High");

        var dto = new ScanSessionDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [candidate],
            new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero));

        Assert.Single(dto.Candidates);
        Assert.Same(candidate, dto.Candidates[0]);
        Assert.Equal(candidate.Id, dto.Candidates[0].Id);
        Assert.Equal("Charlotte's Web", dto.Candidates[0].DisplayTitle);
    }
}
