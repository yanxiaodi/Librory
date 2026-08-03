using System.Text.Json;
using Librory.Application.Metadata;
using Librory.Application.Recognition;

namespace Librory.Infrastructure.Recognition;

public sealed class BookRecognitionPipeline : IBookRecognitionPipeline
{
    private const int MaxMetadataLookups = 5;
    private const decimal FallbackConfidenceThreshold = 0.65m;

    private readonly IOcrTextExtractionService _ocrTextExtractionService;
    private readonly IBookMetadataSearchService _bookMetadataSearchService;
    private readonly IVisionFallbackService _visionFallbackService;
    private readonly BookTitleCandidateRanker _ranker;

    public BookRecognitionPipeline(
        IOcrTextExtractionService ocrTextExtractionService,
        IBookMetadataSearchService bookMetadataSearchService,
        IVisionFallbackService visionFallbackService,
        BookTitleCandidateRanker ranker)
    {
        _ocrTextExtractionService = ocrTextExtractionService;
        _bookMetadataSearchService = bookMetadataSearchService;
        _visionFallbackService = visionFallbackService;
        _ranker = ranker;
    }

    public async Task<BookRecognitionJobResult> RecognizeAsync(
        string sourcePhotoPath,
        string? language,
        CancellationToken cancellationToken)
    {
        var textBlocks = await _ocrTextExtractionService.ExtractAsync(sourcePhotoPath, cancellationToken);
        var ranked = _ranker.Rank(textBlocks).ToList();
        if (ShouldUseFallback(ranked))
        {
            var fallbackTitles = await _visionFallbackService.SuggestCandidateTitlesAsync(sourcePhotoPath, textBlocks, cancellationToken);
            ranked = MergeFallbackCandidates(ranked, fallbackTitles);
        }

        ranked = ranked.Take(MaxMetadataLookups).ToList();

        var warnings = new List<string>();
        var enrichedCandidates = new List<BookRecognitionCandidateDto>(ranked.Count);

        foreach (var candidate in ranked)
        {
            IReadOnlyList<BookMetadataCandidate> metadataMatches = [];

            try
            {
                var metadataResult = await _bookMetadataSearchService.SearchByTitleAsync(
                    candidate.DisplayTitle,
                    language,
                    5,
                    cancellationToken);

                metadataMatches = metadataResult.Candidates;
            }
            catch (Exception exception) when (exception is ArgumentException or HttpRequestException or JsonException or InvalidOperationException)
            {
                warnings.Add($"Metadata lookup failed for '{candidate.DisplayTitle}': {exception.Message}");
            }

            enrichedCandidates.Add(candidate with { MetadataMatches = metadataMatches });
        }

        return new BookRecognitionJobResult(sourcePhotoPath, enrichedCandidates, warnings);
    }

    private static bool ShouldUseFallback(IReadOnlyList<BookRecognitionCandidateDto> ranked)
    {
        if (ranked.Count == 0)
        {
            return true;
        }

        return ranked[0].Rank < (int)(FallbackConfidenceThreshold * 1000m)
            || ranked.Count < 3;
    }

    private static List<BookRecognitionCandidateDto> MergeFallbackCandidates(
        IReadOnlyList<BookRecognitionCandidateDto> ranked,
        IReadOnlyList<string> fallbackTitles)
    {
        var merged = new List<BookRecognitionCandidateDto>(ranked);
        var seenTitles = new HashSet<string>(ranked.Select(candidate => candidate.DisplayTitle), StringComparer.OrdinalIgnoreCase);

        foreach (var fallbackTitle in fallbackTitles)
        {
            if (string.IsNullOrWhiteSpace(fallbackTitle) || !seenTitles.Add(fallbackTitle))
            {
                continue;
            }

            merged.Add(new BookRecognitionCandidateDto(
                Guid.NewGuid(),
                fallbackTitle.Trim(),
                fallbackTitle.Trim(),
                450,
                []));
        }

        return merged
            .OrderByDescending(candidate => candidate.Rank)
            .ThenBy(candidate => candidate.DisplayTitle, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
