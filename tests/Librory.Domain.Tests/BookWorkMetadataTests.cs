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

    [Fact]
    public void Localized_text_prefers_chinese_when_requested_and_available()
    {
        var value = new LocalizedText("Charlotte's Web", "夏洛的网");

        Assert.Equal("夏洛的网", value.GetValue(PreferredLanguage.Chinese));
        Assert.Equal("Charlotte's Web", value.GetValue(PreferredLanguage.English));
    }

    [Fact]
    public void Localized_text_falls_back_to_english_when_chinese_is_missing()
    {
        var value = new LocalizedText("Charlotte's Web");

        Assert.Equal("Charlotte's Web", value.GetValue(PreferredLanguage.Chinese));
        Assert.Equal("Charlotte's Web", value.GetValue(PreferredLanguage.English));
    }

    [Fact]
    public void Localized_text_falls_back_to_english_when_chinese_is_whitespace()
    {
        var value = new LocalizedText("Charlotte's Web", "   ");

        Assert.Equal("Charlotte's Web", value.GetValue(PreferredLanguage.Chinese));
    }
}
