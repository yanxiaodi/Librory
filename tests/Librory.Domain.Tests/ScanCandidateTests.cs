using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public class ScanCandidateTests
{
    [Fact]
    public void ScanCandidate_starts_pending_and_can_be_purchased_only_once()
    {
        var candidate = ScanCandidate.Create("Charlotte's Web", confidenceLabel: "High", recognitionRank: 2);
        var copyId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        Assert.Equal(2, candidate.RecognitionRank);
        Assert.Equal(PurchaseStatus.Pending, candidate.PurchaseStatus);

        candidate.MarkPurchased(copyId, requestId, new DateTimeOffset(2026, 9, 14, 1, 0, 0, TimeSpan.Zero));

        Assert.Equal(PurchaseStatus.Purchased, candidate.PurchaseStatus);
        Assert.Equal(copyId, candidate.PurchasedBookCopyId);
        Assert.Equal(requestId, candidate.PurchaseRequestId);
        Assert.Throws<InvalidOperationException>(() => candidate.MarkPurchased(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ScanCandidate_replacing_metadata_snapshot_clears_stale_duplicate_state()
    {
        var candidate = ScanCandidate.Create(
            "Charlotte's Web",
            confidenceLabel: "High",
            isAlreadyOwned: true,
            duplicateMessage: "Already in the family");

        candidate.ReplaceMetadataMatches("{\"schemaVersion\":1,\"matches\":[]}");

        Assert.Equal("{\"schemaVersion\":1,\"matches\":[]}", candidate.MetadataMatchesJson);
        Assert.False(candidate.IsAlreadyOwned);
        Assert.Null(candidate.DuplicateMessage);
    }

    [Fact]
    public void Initial_metadata_snapshot_can_preserve_initial_duplicate_state()
    {
        var candidate = ScanCandidate.Create(
            "Charlotte's Web",
            confidenceLabel: "High",
            isAlreadyOwned: true,
            duplicateMessage: "Already in the family");

        candidate.ReplaceMetadataMatches(
            "{\"schemaVersion\":1,\"matches\":[]}",
            resetReviewState: false);

        Assert.True(candidate.IsAlreadyOwned);
        Assert.Equal("Already in the family", candidate.DuplicateMessage);
    }

    [Fact]
    public void Title_only_correction_can_clear_old_snapshot_without_clearing_new_review_state()
    {
        var candidate = ScanCandidate.Create("Old title", confidenceLabel: "High");
        candidate.ReplaceMetadataMatches("{\"schemaVersion\":1,\"matches\":[{\"title\":\"Old title\"}]}", resetReviewState: false);
        candidate.ApplyCorrection("New title", "Medium", duplicateMessage: "Review this title");

        candidate.ReplaceMetadataMatches(null, resetReviewState: false);

        Assert.Null(candidate.MetadataMatchesJson);
        Assert.Equal("Review this title", candidate.DuplicateMessage);
    }

    [Fact]
    public void Purchased_scan_candidate_rejects_correction_and_snapshot_replacement()
    {
        var candidate = ScanCandidate.Create("Charlotte's Web", confidenceLabel: "High");
        candidate.MarkPurchased(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => candidate.ApplyCorrection("Matilda", confidenceLabel: "High"));
        Assert.Throws<InvalidOperationException>(() => candidate.ReplaceMetadataMatches("{}"));
    }

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
    public void ScanCandidate_apply_correction_trims_and_updates_values()
    {
        var candidate = ScanCandidate.Create(
            "Charlotte's Web",
            confidenceLabel: "High",
            author: "E. B. White",
            recommendationScore: 0.92m,
            isAlreadyOwned: true,
            duplicateMessage: "Already owned by the family");
        var originalId = candidate.Id;

        candidate.ApplyCorrection(
            "  Matilda  ",
            confidenceLabel: "  Medium  ",
            author: "  Roald Dahl  ",
            recommendationScore: 0.84m,
            isAlreadyOwned: false,
            duplicateMessage: "  Recheck after correction  ");

        Assert.Equal(originalId, candidate.Id);
        Assert.Equal("Matilda", candidate.DisplayTitle);
        Assert.Equal("Roald Dahl", candidate.Author);
        Assert.Equal(0.84m, candidate.RecommendationScore);
        Assert.False(candidate.IsAlreadyOwned);
        Assert.Equal("Recheck after correction", candidate.DuplicateMessage);
        Assert.Equal("Medium", candidate.ConfidenceLabel);
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

    [Fact]
    public void ScanCandidate_create_rejects_negative_recognition_rank()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ScanCandidate.Create(
            "Charlotte's Web",
            confidenceLabel: "Low",
            recognitionRank: -1));
    }

    [Fact]
    public void ScanCandidate_apply_correction_rejects_blank_required_fields()
    {
        var candidate = ScanCandidate.Create("Charlotte's Web", confidenceLabel: "High");

        Assert.Throws<ArgumentException>(() => candidate.ApplyCorrection(" ", confidenceLabel: "High"));
        Assert.Throws<ArgumentException>(() => candidate.ApplyCorrection("Charlotte's Web", confidenceLabel: " "));
    }

    [Fact]
    public void ScanCandidate_apply_correction_rejects_out_of_range_recommendation_score()
    {
        var candidate = ScanCandidate.Create("Charlotte's Web", confidenceLabel: "High");

        Assert.Throws<ArgumentOutOfRangeException>(() => candidate.ApplyCorrection("Charlotte's Web", confidenceLabel: "Low", recommendationScore: -0.01m));
        Assert.Throws<ArgumentOutOfRangeException>(() => candidate.ApplyCorrection("Charlotte's Web", confidenceLabel: "Low", recommendationScore: 1.01m));
    }
}
