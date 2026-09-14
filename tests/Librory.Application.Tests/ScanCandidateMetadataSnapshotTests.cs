using Librory.Application.Metadata;
using Librory.Application.Scanning;
using Xunit;

namespace Librory.Application.Tests;

public sealed class ScanCandidateMetadataSnapshotTests
{
    [Fact]
    public void Metadata_snapshot_round_trips_provider_matches_and_evidence()
    {
        var matches = new[]
        {
            new BookMetadataCandidate(
                "GoogleBooks",
                "g:123",
                "Charlotte's Web",
                null,
                ["E. B. White"],
                "HarperCollins",
                "2006",
                "en",
                "A classic story.",
                null,
                "9780061124952",
                "https://example.test/cover.jpg",
                "https://example.test/book"),
        };

        var json = ScanCandidateMetadataSnapshotSerializer.Serialize(matches, "Matched the title and author on the spine.");
        var snapshot = ScanCandidateMetadataSnapshotSerializer.Deserialize(json);

        Assert.NotNull(snapshot);
        Assert.Equal(1, snapshot!.SchemaVersion);
        Assert.Equal("Matched the title and author on the spine.", snapshot.EvidenceText);
        Assert.Single(snapshot.Matches);
        Assert.Equal("g:123", snapshot.Matches[0].SourceId);
        Assert.Equal("9780061124952", snapshot.Matches[0].Isbn13);
    }

    [Fact]
    public void Metadata_snapshot_deserializer_returns_null_for_missing_snapshot()
    {
        Assert.Null(ScanCandidateMetadataSnapshotSerializer.Deserialize(null));
        Assert.Null(ScanCandidateMetadataSnapshotSerializer.Deserialize(" "));
    }
}
