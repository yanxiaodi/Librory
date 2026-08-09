using System.ClientModel;
using System.Text.Json;
using Librory.Application.Metadata;
using Librory.Application.Recognition;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Librory.Infrastructure.Recognition;

/// <summary>
/// Recognition pipeline that uses a Microsoft Agent Framework <see cref="AIAgent"/> to extract
/// structured book candidates from a shelf photo, then enriches and re-ranks them with Google Books.
/// The two steps are deterministic and sequential, so they are wired directly rather than through
/// the Microsoft.Agents.AI.Workflows graph builder, which targets branching/checkpointed orchestration.
/// </summary>
public sealed class BookRecognitionAgentWorkflow : IBookRecognitionPipeline
{
    private const int MaxCandidates = 8;
    private const int MaxMetadataMatchesPerCandidate = 3;

    private const string AgentInstructions = """
        You are a book-spine recognition assistant. You will be shown a photo of a bookshelf or a stack of books.
        Identify each distinct book you can see and report it as a structured candidate.
        For every candidate, provide:
        - title: the book title exactly as it appears on the spine or cover.
        - author: the author name if it is visible near the title, otherwise null.
        - evidenceText: the exact text you read from the image that supports this candidate.
        - confidence: a number between 0 and 1 for how confident you are this is a real book title, and not a
          publisher name, review quote, series label, or other noise.
        Only report genuine book titles that are actually visible in the photo. Do not invent books you cannot see.
        Return at most 8 candidates, ordered by confidence descending.
        """;

    private readonly IBookVisionChatClientFactory _chatClientFactory;
    private readonly IBookMetadataSearchService _bookMetadataSearchService;
    private readonly ILogger<BookRecognitionAgentWorkflow> _logger;

    public BookRecognitionAgentWorkflow(
        IBookVisionChatClientFactory chatClientFactory,
        IBookMetadataSearchService bookMetadataSearchService,
        ILogger<BookRecognitionAgentWorkflow> logger)
    {
        _chatClientFactory = chatClientFactory;
        _bookMetadataSearchService = bookMetadataSearchService;
        _logger = logger;
    }

    public async Task<BookRecognitionJobResult> RecognizeAsync(
        string sourcePhotoPath,
        string? language,
        CancellationToken cancellationToken)
    {
        var warnings = new List<string>();
        var candidates = await ExtractCandidatesAsync(sourcePhotoPath, warnings, cancellationToken);
        var enrichedCandidates = new List<BookRecognitionCandidateDto>();

        foreach (var candidate in candidates.Take(MaxCandidates))
        {
            IReadOnlyList<BookMetadataCandidate> metadataMatches = [];

            try
            {
                var metadataResult = await _bookMetadataSearchService.SearchByTitleAsync(
                    candidate.Title,
                    language,
                    5,
                    cancellationToken);

                metadataMatches = RankMetadataMatches(candidate, metadataResult.Candidates);
            }
            catch (Exception exception) when (exception is ArgumentException or HttpRequestException or JsonException or InvalidOperationException)
            {
                warnings.Add($"Metadata lookup failed for '{candidate.Title}': {exception.Message}");
            }

            enrichedCandidates.Add(new BookRecognitionCandidateDto(
                Guid.NewGuid(),
                candidate.Title,
                candidate.EvidenceText ?? candidate.Title,
                (int)Math.Round(candidate.Confidence * 1000m, MidpointRounding.AwayFromZero),
                metadataMatches));
        }

        _logger.LogInformation(
            "Book recognition workflow produced {CandidateCount} candidate(s) and {WarningCount} warning(s) for {SourcePhotoPath}.",
            enrichedCandidates.Count,
            warnings.Count,
            sourcePhotoPath);

        return new BookRecognitionJobResult(sourcePhotoPath, enrichedCandidates, warnings);
    }

    private async Task<IReadOnlyList<BookCandidate>> ExtractCandidatesAsync(
        string sourcePhotoPath,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var chatClient = _chatClientFactory.CreateChatClient();
        if (chatClient is null)
        {
            _logger.LogWarning("Agent Framework Azure OpenAI is not configured for {SourcePhotoPath}.", sourcePhotoPath);
            return [];
        }

        AIAgent agent = chatClient.AsAIAgent(instructions: AgentInstructions, name: "BookVisionAgent");

        try
        {
            ChatMessage message = new(
                ChatRole.User,
                [
                    new TextContent("Identify the book candidates visible in this photo."),
                    await DataContent.LoadFromAsync(sourcePhotoPath, cancellationToken: cancellationToken),
                ]);

            AgentResponse<VisionCandidateExtractionResult> response =
                await agent.RunAsync<VisionCandidateExtractionResult>(message, cancellationToken: cancellationToken);

            var extracted = response.Result.Candidates
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Title))
                .Select(candidate => new BookCandidate(
                    candidate.Title.Trim(),
                    string.IsNullOrWhiteSpace(candidate.Author) ? null : candidate.Author.Trim(),
                    candidate.EvidenceText,
                    (decimal)Math.Clamp(candidate.Confidence, 0d, 1d)))
                .OrderByDescending(candidate => candidate.Confidence)
                .ToList();

            _logger.LogInformation(
                "Vision agent extracted {CandidateCount} structured candidate(s) for {SourcePhotoPath}.",
                extracted.Count,
                sourcePhotoPath);

            return extracted;
        }
        catch (Exception exception) when (exception is ClientResultException or IOException or InvalidOperationException or JsonException)
        {
            warnings.Add($"Vision candidate extraction failed: {exception.Message}");
            _logger.LogError(exception, "Vision candidate extraction failed for {SourcePhotoPath}.", sourcePhotoPath);
            return [];
        }
    }

    private static IReadOnlyList<BookMetadataCandidate> RankMetadataMatches(
        BookCandidate candidate,
        IReadOnlyList<BookMetadataCandidate> matches)
    {
        if (matches.Count <= 1)
        {
            return matches;
        }

        return matches
            .OrderByDescending(match => ScoreMetadataMatch(candidate, match))
            .Take(MaxMetadataMatchesPerCandidate)
            .ToList();
    }

    private static int ScoreMetadataMatch(BookCandidate candidate, BookMetadataCandidate match)
    {
        var score = 0;
        var normalizedCandidateTitle = NormalizeForComparison(candidate.Title);
        var normalizedMatchTitle = NormalizeForComparison(match.Title);

        if (normalizedCandidateTitle.Length > 0 && normalizedCandidateTitle == normalizedMatchTitle)
        {
            score += 3;
        }
        else if (normalizedMatchTitle.Contains(normalizedCandidateTitle, StringComparison.Ordinal)
            || normalizedCandidateTitle.Contains(normalizedMatchTitle, StringComparison.Ordinal))
        {
            score += 1;
        }

        if (!string.IsNullOrWhiteSpace(candidate.Author))
        {
            var normalizedCandidateAuthor = NormalizeForComparison(candidate.Author);
            var authorAgrees = match.Authors.Any(author =>
            {
                var normalizedMatchAuthor = NormalizeForComparison(author);
                return normalizedMatchAuthor.Contains(normalizedCandidateAuthor, StringComparison.Ordinal)
                    || normalizedCandidateAuthor.Contains(normalizedMatchAuthor, StringComparison.Ordinal);
            });

            if (authorAgrees)
            {
                score += 2;
            }
            else if (match.Authors.Count > 0)
            {
                score -= 1;
            }
        }

        return score;
    }

    private static string NormalizeForComparison(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }
}