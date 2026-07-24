using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public class BookWorkMetadataTests
{
    [Fact]
    public void Book_work_can_store_summary_and_provenance()
    {
        var work = BookWork.Create("Charlotte's Web", "E. B. White");
        var provenance = new MetadataProvenance
        {
            Source = "library-of-congress",
            SourceId = "2011029476",
            Confidence = 1.0m,
            CapturedAt = new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero),
        };

        work.Summary = new LocalizedText("A pig named Wilbur...", "一只叫威尔伯的猪...");
        work.SummaryProvenance = provenance;
        work.CanonicalAuthorProvenance = provenance;

        Assert.Equal("A pig named Wilbur...", work.Summary!.English);
        Assert.Equal("一只叫威尔伯的猪...", work.Summary!.Chinese);
        Assert.Same(provenance, work.SummaryProvenance);
        Assert.Same(provenance, work.CanonicalAuthorProvenance);
    }

    [Fact]
    public void Book_work_allows_optional_metadata_to_remain_null()
    {
        var work = BookWork.Create("Charlotte's Web");

        Assert.Null(work.Summary);
        Assert.Null(work.SummaryProvenance);
        Assert.Null(work.CanonicalAuthorProvenance);
    }
}
