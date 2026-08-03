using Librory.Application.Metadata;
using Librory.Application.Recognition;
using Librory.Infrastructure.Recognition;
using Xunit;

namespace Librory.Api.Tests;

public sealed class BookRecognitionPipelineTests
{
    [Fact]
    public void Ranker_prefers_title_like_spans_and_downranks_noise()
    {
        var ranker = new BookTitleCandidateRanker();

        var result = ranker.Rank(new[]
        {
            new RecognizedTextBlock("The Left Hand of Darkness", 0.98m, 100, 120, 520, 180, false),
            new RecognizedTextBlock("Ursula K. Le Guin", 0.95m, 110, 190, 430, 230, false),
            new RecognizedTextBlock("A masterpiece of science fiction", 0.82m, 80, 240, 560, 290, false),
        });

        Assert.Equal("The Left Hand of Darkness", result.First().DisplayTitle);
        Assert.DoesNotContain(result, candidate => candidate.DisplayTitle.Contains("masterpiece", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Pipeline_uses_ocr_and_enriches_top_candidates_with_metadata()
    {
        var ocr = new FakeOcrTextExtractionService(new[]
        {
            new RecognizedTextBlock("Dune", 0.99m, 100, 100, 240, 180, false),
            new RecognizedTextBlock("Frank Herbert", 0.97m, 100, 200, 260, 240, false),
        });

        var metadata = new FakeBookMetadataSearchService("Dune", new[]
        {
            new BookMetadataCandidate("google-books", "source-1", "Dune", null, ["Frank Herbert"], "Ace", "1965", "en", null, "9780441013593", "9780441013593", null, null),
        });

        var pipeline = new BookRecognitionPipeline(ocr, metadata, new FakeVisionFallbackService([]), new BookTitleCandidateRanker());
        var result = await pipeline.RecognizeAsync("/tmp/shelf.jpg", "en", CancellationToken.None);

        Assert.Contains("Dune", metadata.TitlesQueried);
        Assert.Contains(result.Candidates, candidate => candidate.DisplayTitle == "Dune" && candidate.MetadataMatches.Count == 1);
    }

    [Fact]
    public async Task Pipeline_keeps_candidates_when_metadata_lookup_fails()
    {
        var ocr = new FakeOcrTextExtractionService(new[]
        {
            new RecognizedTextBlock("Dune", 0.99m, 100, 100, 240, 180, false),
        });

        var metadata = new ThrowingBookMetadataSearchService();
        var pipeline = new BookRecognitionPipeline(ocr, metadata, new FakeVisionFallbackService([]), new BookTitleCandidateRanker());

        var result = await pipeline.RecognizeAsync("/tmp/shelf.jpg", "en", CancellationToken.None);

        Assert.Single(result.Candidates);
        Assert.Empty(result.Candidates[0].MetadataMatches);
        Assert.NotEmpty(result.Warnings);
    }

    private sealed class FakeOcrTextExtractionService : IOcrTextExtractionService
    {
        private readonly IReadOnlyList<RecognizedTextBlock> _blocks;

        public FakeOcrTextExtractionService(IReadOnlyList<RecognizedTextBlock> blocks)
        {
            _blocks = blocks;
        }

        public Task<IReadOnlyList<RecognizedTextBlock>> ExtractAsync(string sourcePhotoPath, CancellationToken cancellationToken)
        {
            return Task.FromResult(_blocks);
        }
    }

    private sealed class FakeBookMetadataSearchService : IBookMetadataSearchService
    {
        private readonly string _expectedTitle;
        private readonly IReadOnlyList<BookMetadataCandidate> _candidates;
        private readonly List<string> _titlesQueried = [];

        public FakeBookMetadataSearchService(string expectedTitle, IReadOnlyList<BookMetadataCandidate> candidates)
        {
            _expectedTitle = expectedTitle;
            _candidates = candidates;
        }

        public IReadOnlyList<string> TitlesQueried => _titlesQueried;

        public Task<BookMetadataSearchResult> SearchByTitleAsync(string title, string? language, int maxResults, CancellationToken cancellationToken)
        {
            _titlesQueried.Add(title);

            if (title.Equals(_expectedTitle, StringComparison.OrdinalIgnoreCase))
            {
                Assert.Equal("en", language);
                Assert.Equal(5, maxResults);
                return Task.FromResult(new BookMetadataSearchResult(title, _candidates.Count, _candidates));
            }

            return Task.FromResult(new BookMetadataSearchResult(title, 0, []));
        }
    }

    private sealed class ThrowingBookMetadataSearchService : IBookMetadataSearchService
    {
        public Task<BookMetadataSearchResult> SearchByTitleAsync(string title, string? language, int maxResults, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Service unavailable.");
        }
    }

    private sealed class FakeVisionFallbackService : IVisionFallbackService
    {
        private readonly IReadOnlyList<string> _titles;

        public FakeVisionFallbackService(IReadOnlyList<string> titles)
        {
            _titles = titles;
        }

        public Task<IReadOnlyList<string>> SuggestCandidateTitlesAsync(
            string sourcePhotoPath,
            IReadOnlyList<RecognizedTextBlock> recognizedText,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_titles);
        }
    }
}
