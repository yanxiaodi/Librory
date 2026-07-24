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
        Assert.Empty(session.Candidates);
        Assert.Equal(TimeSpan.FromDays(7), session.ExpiresAt - session.CreatedAt);
        Assert.False(session.IsExpired(session.CreatedAt));
        Assert.True(session.IsExpired(session.ExpiresAt.AddSeconds(1)));
    }

    [Fact]
    public void Family_start_scan_session_accepts_custom_retention_and_candidates()
    {
        var family = Family.Create("The Yans");

        var session = family.StartScanSession(TimeSpan.FromDays(3));
        var candidate = new ScanCandidate
        {
            DisplayTitle = "Charlotte's Web",
            Author = "E. B. White",
            RecommendationScore = 0.92m,
            IsAlreadyOwned = true,
            DuplicateMessage = "Already owned by the family",
            ConfidenceLabel = "High",
        };

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
    }

    [Fact]
    public void Scan_session_add_candidate_throws_when_candidate_is_null()
    {
        var family = Family.Create("The Yans");
        var session = family.StartScanSession();

        Assert.Throws<ArgumentNullException>(() => session.AddCandidate(null!));
    }

    [Fact]
    public void Scan_session_throws_when_retention_window_is_not_positive()
    {
        var family = Family.Create("The Yans");

        Assert.Throws<ArgumentOutOfRangeException>(() => family.StartScanSession(TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => family.StartScanSession(TimeSpan.FromDays(-1)));
    }
}
