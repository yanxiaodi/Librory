using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Librory.Api.Contracts;
using Librory.Application.Metadata;
using Librory.Infrastructure.Metadata.GoogleBooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Librory.Api.Tests;

public sealed class BookMetadataSearchTests
{
    [Fact]
    public async Task Search_endpoint_returns_normalized_metadata_results()
    {
        await using var factory = await ApiFactory.CreateAsync();
        var fakeService = new FakeBookMetadataSearchService();

        using var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBookMetadataSearchService>();
                services.AddSingleton<IBookMetadataSearchService>(fakeService);
            });
        });

        using var client = configuredFactory.CreateClient();

        var response = await client.GetAsync("/api/book-metadata/search?title=The%20Hobbit&language=en&maxResults=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<BookMetadataSearchResponse>();
        Assert.NotNull(payload);
        Assert.Equal("The Hobbit", payload!.Query);
        Assert.Equal(1, payload.TotalItems);
        Assert.Single(payload.Candidates);

        var candidate = payload.Candidates[0];
        Assert.Equal("GoogleBooks", candidate.Source);
        Assert.Equal("volume-1", candidate.SourceId);
        Assert.Equal("The Hobbit", candidate.Title);
        Assert.Equal("Allen & Unwin", candidate.Publisher);
        Assert.Equal("9780000000002", candidate.Isbn13);

        Assert.Equal("The Hobbit", fakeService.LastTitle);
        Assert.Equal("en", fakeService.LastLanguage);
        Assert.Equal(5, fakeService.LastMaxResults);
    }

    [Fact]
    public async Task Google_books_provider_searches_by_title_and_maps_results()
    {
        var handler = new StubHttpMessageHandler();
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://www.googleapis.com/books/v1/"),
        };

        using var serviceProvider = BuildProvider("test-api-key");
        var service = new GoogleBooksMetadataSearchService(client, serviceProvider.GetRequiredService<IOptions<GoogleBooksOptions>>());

        var result = await service.SearchByTitleAsync("The Hobbit", "en", 5, CancellationToken.None);

        Assert.Equal("The Hobbit", result.Query);
        Assert.Equal(1, result.TotalItems);
        Assert.Single(result.Candidates);

        var candidate = result.Candidates[0];
        Assert.Equal("GoogleBooks", candidate.Source);
        Assert.Equal("volume-1", candidate.SourceId);
        Assert.Equal("The Hobbit", candidate.Title);
        Assert.Equal("J.R.R. Tolkien", candidate.Authors[0]);
        Assert.Equal("9780000000002", candidate.Isbn13);
        Assert.Equal("https://books.google.com/books?id=volume-1", candidate.InfoUrl);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("/books/v1/volumes", handler.LastRequestUri!.AbsolutePath);
        Assert.Contains("q=The%20Hobbit", handler.LastRequestUri.Query);
        Assert.Contains("langRestrict=en", handler.LastRequestUri.Query);
        Assert.Contains("maxResults=5", handler.LastRequestUri.Query);
        Assert.Contains("key=test-api-key", handler.LastRequestUri.Query);
        Assert.Contains("fields=", handler.LastRequestUri.Query);
    }

    private static ServiceProvider BuildProvider(string apiKey)
    {
        var services = new ServiceCollection();
        services.AddOptions<GoogleBooksOptions>().Configure(options => options.ApiKey = apiKey);
        return services.BuildServiceProvider();
    }

    private sealed class FakeBookMetadataSearchService : IBookMetadataSearchService
    {
        public string? LastTitle { get; private set; }

        public string? LastLanguage { get; private set; }

        public int LastMaxResults { get; private set; }

        public Task<BookMetadataSearchResult> SearchByTitleAsync(
            string title,
            string? language,
            int maxResults,
            CancellationToken cancellationToken)
        {
            LastTitle = title;
            LastLanguage = language;
            LastMaxResults = maxResults;

            var candidates = new[]
            {
                new BookMetadataCandidate(
                    "GoogleBooks",
                    "volume-1",
                    "The Hobbit",
                    null,
                    ["J.R.R. Tolkien"],
                    "Allen & Unwin",
                    "1937",
                    "en",
                    "A hobbit goes on an unexpected journey.",
                    "0000000000",
                    "9780000000002",
                    "https://example.com/thumb.jpg",
                    "https://books.google.com/books?id=volume-1"),
            };

            return Task.FromResult(new BookMetadataSearchResult(title, 1, candidates));
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;

            var payload = new
            {
                totalItems = 1,
                items = new[]
                {
                    new
                    {
                        id = "volume-1",
                        volumeInfo = new
                        {
                            title = "The Hobbit",
                            subtitle = "There and Back Again",
                            authors = new[] { "J.R.R. Tolkien" },
                            publisher = "Allen & Unwin",
                            publishedDate = "1937",
                            language = "en",
                            description = "A hobbit goes on an unexpected journey.",
                            industryIdentifiers = new[]
                            {
                                new { type = "ISBN_10", identifier = "0000000000" },
                                new { type = "ISBN_13", identifier = "9780000000002" },
                            },
                            imageLinks = new
                            {
                                thumbnail = "https://example.com/thumb.jpg",
                            },
                            infoLink = "https://books.google.com/books?id=volume-1",
                        },
                    },
                },
            };

            var content = JsonContent.Create(payload, options: JsonOptions);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content,
            };

            return Task.FromResult(response);
        }
    }
}
