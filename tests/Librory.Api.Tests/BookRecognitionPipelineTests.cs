using Librory.Application.Metadata;
using Librory.Application.Recognition;
using Librory.Infrastructure.Recognition;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
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
    public async Task Workflow_returns_no_candidates_when_agent_framework_is_not_configured()
    {
        var pipeline = new BookRecognitionAgentWorkflow(
            new FakeBookVisionChatClientFactory(chatClient: null),
            new FakeBookMetadataSearchService("Dune", []),
            NullLogger<BookRecognitionAgentWorkflow>.Instance);

        await using var photo = new TempPhotoFile();
        var result = await pipeline.RecognizeAsync(photo.Path, "en", CancellationToken.None);

        Assert.Empty(result.Candidates);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task Workflow_extracts_structured_candidates_and_enriches_with_metadata()
    {
        const string ResponseJson = """
            {"candidates":[{"title":"Dune","author":"Frank Herbert","evidenceText":"DUNE / Frank Herbert","confidence":0.95}]}
            """;

        var metadata = new FakeBookMetadataSearchService("Dune", new[]
        {
            new BookMetadataCandidate("google-books", "source-1", "Dune", null, ["Frank Herbert"], "Ace", "1965", "en", null, "9780441013593", "9780441013593", null, null),
        });

        var pipeline = new BookRecognitionAgentWorkflow(
            new FakeBookVisionChatClientFactory(new FakeChatClient(ResponseJson)),
            metadata,
            NullLogger<BookRecognitionAgentWorkflow>.Instance);

        await using var photo = new TempPhotoFile();
        var result = await pipeline.RecognizeAsync(photo.Path, "en", CancellationToken.None);

        Assert.Contains("Dune", metadata.TitlesQueried);
        Assert.Contains(result.Candidates, candidate => candidate.DisplayTitle == "Dune" && candidate.MetadataMatches.Count == 1);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task Workflow_keeps_candidate_and_adds_warning_when_metadata_lookup_fails()
    {
        const string ResponseJson = """
            {"candidates":[{"title":"Dune","author":null,"evidenceText":"DUNE","confidence":0.9}]}
            """;

        var pipeline = new BookRecognitionAgentWorkflow(
            new FakeBookVisionChatClientFactory(new FakeChatClient(ResponseJson)),
            new ThrowingBookMetadataSearchService(),
            NullLogger<BookRecognitionAgentWorkflow>.Instance);

        await using var photo = new TempPhotoFile();
        var result = await pipeline.RecognizeAsync(photo.Path, "en", CancellationToken.None);

        Assert.Single(result.Candidates);
        Assert.Empty(result.Candidates[0].MetadataMatches);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public async Task Workflow_reranks_metadata_matches_by_title_and_author_agreement()
    {
        const string ResponseJson = """
            {"candidates":[{"title":"Dune","author":"Frank Herbert","evidenceText":"DUNE","confidence":0.9}]}
            """;

        var metadata = new FakeBookMetadataSearchService("Dune", new[]
        {
            new BookMetadataCandidate("google-books", "wrong-author", "Dune", null, ["Someone Else"], null, null, "en", null, null, null, null, null),
            new BookMetadataCandidate("google-books", "right-author", "Dune", null, ["Frank Herbert"], null, null, "en", null, null, null, null, null),
        });

        var pipeline = new BookRecognitionAgentWorkflow(
            new FakeBookVisionChatClientFactory(new FakeChatClient(ResponseJson)),
            metadata,
            NullLogger<BookRecognitionAgentWorkflow>.Instance);

        await using var photo = new TempPhotoFile();
        var result = await pipeline.RecognizeAsync(photo.Path, "en", CancellationToken.None);

        var topMatch = Assert.Single(result.Candidates).MetadataMatches[0];
        Assert.Equal("right-author", topMatch.SourceId);
    }

    private sealed class FakeBookVisionChatClientFactory : IBookVisionChatClientFactory
    {
        private readonly IChatClient? _chatClient;

        public FakeBookVisionChatClientFactory(IChatClient? chatClient)
        {
            _chatClient = chatClient;
        }

        public IChatClient? CreateChatClient() => _chatClient;
    }

    private sealed class FakeChatClient : IChatClient
    {
        private readonly string _responseJson;

        public FakeChatClient(string responseJson)
        {
            _responseJson = responseJson;
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, _responseJson)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Streaming is not used by the recognition workflow.");
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private sealed class TempPhotoFile : IAsyncDisposable
    {
        public TempPhotoFile()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
            File.WriteAllBytes(Path, [0xFF, 0xD8, 0xFF, 0xD9]);
        }

        public string Path { get; }

        public ValueTask DisposeAsync()
        {
            File.Delete(Path);
            return ValueTask.CompletedTask;
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
}
