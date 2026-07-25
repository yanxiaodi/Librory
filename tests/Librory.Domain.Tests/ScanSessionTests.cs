using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public class ScanSessionTests
{
    [Fact]
    public void Family_start_scan_session_registers_session_with_default_retention()
    {
        var family = Family.Create("The Yans");

        var session = family.StartScanSession();

        Assert.Single(family.ScanSessions);
        Assert.Same(session, family.ScanSessions[0]);
        Assert.Equal(family.Id, session.FamilyId);
        Assert.Equal(string.Empty, session.ShelfPhotoPath);
        Assert.Empty(session.Candidates);
        Assert.Equal(TimeSpan.FromDays(7), session.ExpiresAt - session.CreatedAt);
        Assert.False(session.IsExpired(session.CreatedAt));
        Assert.True(session.IsExpired(session.ExpiresAt.AddSeconds(1)));
    }

    [Fact]
    public void Family_start_scan_session_can_store_shelf_photo_path()
    {
        var family = Family.Create("The Yans");

        var session = family.StartScanSession("shelf-photo.jpg");

        Assert.Equal("shelf-photo.jpg", session.ShelfPhotoPath);
    }

    [Fact]
    public void Family_start_scan_session_accepts_custom_retention_and_candidates()
    {
        var family = Family.Create("The Yans");

        var session = family.StartScanSession("shelf-photo.jpg", TimeSpan.FromDays(3));
        var candidate = ScanCandidate.Create(
            "Charlotte's Web",
            author: "E. B. White",
            recommendationScore: 0.92m,
            isAlreadyOwned: true,
            duplicateMessage: "Already owned by the family",
            confidenceLabel: "High");

        session.AddCandidate(candidate);

        Assert.Equal(TimeSpan.FromDays(3), session.ExpiresAt - session.CreatedAt);
        Assert.Single(session.Candidates);
        Assert.Same(candidate, session.Candidates[0]);
        Assert.Equal("Charlotte's Web", session.Candidates[0].DisplayTitle);
        Assert.Equal("E. B. White", session.Candidates[0].Author);
        Assert.Equal(0.92m, session.Candidates[0].RecommendationScore);
        Assert.True(session.Candidates[0].IsAlreadyOwned);
        Assert.Equal("Already owned by the family", session.Candidates[0].DuplicateMessage);
        Assert.Equal("High", session.Candidates[0].ConfidenceLabel);
        Assert.Equal(family.Id, session.FamilyId);
        Assert.Same(family, session.Family);
        Assert.Equal("shelf-photo.jpg", session.ShelfPhotoPath);
    }

    [Fact]
    public void Scan_session_correct_candidate_updates_only_the_target_candidate()
    {
        var family = Family.Create("The Yans");
        var session = family.StartScanSession("shelf-photo.jpg");
        var firstCandidate = ScanCandidate.Create(
            "Charlotte's Web",
            confidenceLabel: "High",
            author: "E. B. White",
            recommendationScore: 0.92m,
            isAlreadyOwned: true,
            duplicateMessage: "Already owned by the family");
        var secondCandidate = ScanCandidate.Create(
            "Matilda",
            confidenceLabel: "Medium",
            author: "Roald Dahl",
            recommendationScore: 0.78m,
            duplicateMessage: "Different edition available");

        session.AddCandidate(firstCandidate);
        session.AddCandidate(secondCandidate);

        session.CorrectCandidate(
            firstCandidate.Id,
            "  The Spider and the Pig  ",
            confidenceLabel: "  Medium  ",
            author: "  E. B. White  ",
            recommendationScore: 0.87m,
            isAlreadyOwned: false,
            duplicateMessage: "  Recheck duplicate after correction  ");

        Assert.Equal("The Spider and the Pig", firstCandidate.DisplayTitle);
        Assert.Equal("E. B. White", firstCandidate.Author);
        Assert.Equal(0.87m, firstCandidate.RecommendationScore);
        Assert.False(firstCandidate.IsAlreadyOwned);
        Assert.Equal("Recheck duplicate after correction", firstCandidate.DuplicateMessage);
        Assert.Equal("Medium", firstCandidate.ConfidenceLabel);
        Assert.Equal("Matilda", secondCandidate.DisplayTitle);
        Assert.Equal("Medium", secondCandidate.ConfidenceLabel);
        Assert.Equal(2, session.Candidates.Count);
        Assert.Same(firstCandidate, session.Candidates[0]);
        Assert.Same(secondCandidate, session.Candidates[1]);
    }

    [Fact]
    public void Scan_session_correct_candidate_throws_when_candidate_is_missing()
    {
        var family = Family.Create("The Yans");
        var session = family.StartScanSession("shelf-photo.jpg");

        Assert.Throws<InvalidOperationException>(() => session.CorrectCandidate(
            Guid.NewGuid(),
            "Charlotte's Web",
            confidenceLabel: "High"));
    }

    [Fact]
    public void Scan_session_add_candidate_throws_when_candidate_is_null()
    {
        var family = Family.Create("The Yans");
        var session = family.StartScanSession("shelf-photo.jpg");

        Assert.Throws<ArgumentNullException>(() => session.AddCandidate(null!));
    }

    [Fact]
    public void Scan_session_throws_when_retention_window_is_not_positive()
    {
        var family = Family.Create("The Yans");

        Assert.Throws<ArgumentOutOfRangeException>(() => family.StartScanSession("shelf-photo.jpg", TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => family.StartScanSession("shelf-photo.jpg", TimeSpan.FromDays(-1)));
    }
}
