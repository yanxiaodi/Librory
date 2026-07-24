using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public class MetadataProvenanceTests
{
    [Fact]
    public void MetadataProvenance_preserves_all_provided_values()
    {
        var capturedAt = new DateTimeOffset(2026, 7, 24, 13, 45, 0, TimeSpan.Zero);

        var provenance = new MetadataProvenance
        {
            Source = "OpenLibrary",
            SourceId = "ol:12345",
            Confidence = 0.92m,
            CapturedAt = capturedAt,
        };

        Assert.Equal("OpenLibrary", provenance.Source);
        Assert.Equal("ol:12345", provenance.SourceId);
        Assert.Equal(0.92m, provenance.Confidence);
        Assert.Equal(capturedAt, provenance.CapturedAt);
    }

    [Fact]
    public void MetadataProvenance_defaults_all_optional_members_to_null()
    {
        var provenance = new MetadataProvenance();

        Assert.Null(provenance.Source);
        Assert.Null(provenance.SourceId);
        Assert.Null(provenance.Confidence);
        Assert.Null(provenance.CapturedAt);
    }
}
