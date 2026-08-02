using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Librory.Application.Metadata;
using Microsoft.Extensions.Options;

namespace Librory.Infrastructure.Metadata.GoogleBooks;

public sealed class GoogleBooksMetadataSearchService : IBookMetadataSearchService
{
    private const string SourceName = "GoogleBooks";
    private const int MaxAllowedResults = 40;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly GoogleBooksOptions _options;

    public GoogleBooksMetadataSearchService(HttpClient httpClient, IOptions<GoogleBooksOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<BookMetadataSearchResult> SearchByTitleAsync(
        string title,
        string? language,
        int maxResults,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (maxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults), "Maximum results must be positive.");
        }

        var queryTitle = title.Trim();
        var query = BuildQuery(queryTitle, language, Math.Min(maxResults, MaxAllowedResults));

        using var response = await _httpClient.GetAsync(query, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<GoogleBooksSearchResponse>(JsonOptions, cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Google Books returned an empty response.");
        }

        var candidates = (payload.Items ?? Array.Empty<GoogleBooksVolumeItem>())
            .Select(item => MapCandidate(item))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .ToList();

        return new BookMetadataSearchResult(queryTitle, payload.TotalItems, candidates);
    }

    private string BuildQuery(string title, string? language, int maxResults)
    {
        var query = new StringBuilder("volumes?");
        query.Append("q=").Append(Uri.EscapeDataString(title));
        query.Append("&orderBy=relevance");
        query.Append("&printType=books");
        query.Append("&maxResults=").Append(maxResults);
        query.Append("&fields=").Append(Uri.EscapeDataString("totalItems,items(id,volumeInfo(title,subtitle,authors,publisher,publishedDate,language,description,industryIdentifiers(type,identifier),imageLinks/thumbnail,infoLink))"));

        if (!string.IsNullOrWhiteSpace(language))
        {
            query.Append("&langRestrict=").Append(Uri.EscapeDataString(language.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            query.Append("&key=").Append(Uri.EscapeDataString(_options.ApiKey.Trim()));
        }

        return query.ToString();
    }

    private static BookMetadataCandidate? MapCandidate(GoogleBooksVolumeItem item)
    {
        var volumeInfo = item.VolumeInfo;
        if (string.IsNullOrWhiteSpace(volumeInfo.Title))
        {
            return null;
        }

        var identifiers = volumeInfo.IndustryIdentifiers ?? [];
        var isbn13 = identifiers.FirstOrDefault(x => string.Equals(x.Type, "ISBN_13", StringComparison.OrdinalIgnoreCase))?.Identifier;
        var isbn10 = identifiers.FirstOrDefault(x => string.Equals(x.Type, "ISBN_10", StringComparison.OrdinalIgnoreCase))?.Identifier;

        return new BookMetadataCandidate(
            SourceName,
            item.Id,
            volumeInfo.Title.Trim(),
            TrimToNull(volumeInfo.Subtitle),
            (volumeInfo.Authors ?? []).Where(author => !string.IsNullOrWhiteSpace(author)).Select(author => author.Trim()).ToList(),
            TrimToNull(volumeInfo.Publisher),
            TrimToNull(volumeInfo.PublishedDate),
            TrimToNull(volumeInfo.Language),
            TrimToNull(volumeInfo.Description),
            TrimToNull(isbn10),
            TrimToNull(isbn13),
            TrimToNull(volumeInfo.ImageLinks?.Thumbnail),
            TrimToNull(volumeInfo.InfoLink));
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed record GoogleBooksSearchResponse(
        [property: JsonPropertyName("totalItems")] int TotalItems,
        [property: JsonPropertyName("items")] IReadOnlyList<GoogleBooksVolumeItem>? Items);

    private sealed record GoogleBooksVolumeItem(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("volumeInfo")] GoogleBooksVolumeInfo VolumeInfo);

    private sealed record GoogleBooksVolumeInfo(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("subtitle")] string? Subtitle,
        [property: JsonPropertyName("authors")] IReadOnlyList<string>? Authors,
        [property: JsonPropertyName("publisher")] string? Publisher,
        [property: JsonPropertyName("publishedDate")] string? PublishedDate,
        [property: JsonPropertyName("language")] string? Language,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("industryIdentifiers")] IReadOnlyList<GoogleBooksIndustryIdentifier>? IndustryIdentifiers,
        [property: JsonPropertyName("imageLinks")] GoogleBooksImageLinks? ImageLinks,
        [property: JsonPropertyName("infoLink")] string? InfoLink);

    private sealed record GoogleBooksIndustryIdentifier(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("identifier")] string Identifier);

    private sealed record GoogleBooksImageLinks(
        [property: JsonPropertyName("thumbnail")] string? Thumbnail);
}
