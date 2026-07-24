using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public class BookEditionMetadataTests
{
    [Fact]
    public void Book_edition_can_store_subtitle_and_provenance()
    {
        var provenance = new MetadataProvenance
        {
            Source = "OpenLibrary",
            SourceId = "ol:ed123",
            Confidence = 0.88m,
            CapturedAt = new DateTimeOffset(2026, 7, 24, 12, 30, 0, TimeSpan.Zero),
        };

        var edition = new BookEdition
        {
            Subtitle = "A Special Edition",
            SubtitleProvenance = provenance,
            PublicationYearProvenance = provenance,
        };

        Assert.Equal("A Special Edition", edition.Subtitle);
        Assert.Same(provenance, edition.SubtitleProvenance);
        Assert.Same(provenance, edition.PublicationYearProvenance);
    }

    [Fact]
    public void Book_edition_allows_optional_metadata_to_remain_null()
    {
        var edition = new BookEdition();

        Assert.Null(edition.Subtitle);
        Assert.Null(edition.SubtitleProvenance);
        Assert.Null(edition.PublicationYearProvenance);
    }
}
