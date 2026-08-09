using System.Net.Http.Headers;
using System.Globalization;
using System.Text.Json;
using Librory.Application.Recognition;
using Microsoft.Extensions.Options;

namespace Librory.Infrastructure.Recognition;

public sealed class DocumentIntelligenceTextExtractionService : IOcrTextExtractionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IOptions<RecognitionOptions> _options;

    public DocumentIntelligenceTextExtractionService(HttpClient httpClient, IOptions<RecognitionOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<IReadOnlyList<RecognizedTextBlock>> ExtractAsync(
        string sourcePhotoPath,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured(_options.Value.DocumentIntelligence))
        {
            throw new InvalidOperationException("Document Intelligence OCR is not configured.");
        }

        var endpoint = NormalizeEndpoint(_options.Value.DocumentIntelligence.Endpoint);
        var requestUri = new Uri($"{endpoint}/documentintelligence/documentModels/prebuilt-read:analyze?api-version=2024-11-30");

        await using var stream = File.OpenRead(sourcePhotoPath);
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StreamContent(stream),
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        request.Headers.Add("Ocp-Apim-Subscription-Key", _options.Value.DocumentIntelligence.ApiKey);
        request.Headers.Add("Accept", "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        return ParseRecognizedTextBlocks(document.RootElement);
    }

    private static bool IsConfigured(DocumentIntelligenceOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.Endpoint) && !string.IsNullOrWhiteSpace(options.ApiKey);
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        return endpoint.TrimEnd('/');
    }

    private static IReadOnlyList<RecognizedTextBlock> ParseRecognizedTextBlocks(JsonElement root)
    {
        var results = new List<RecognizedTextBlock>();

        if (root.TryGetProperty("analyzeResult", out var analyzeResult)
            && analyzeResult.TryGetProperty("pages", out var pages)
            && pages.ValueKind == JsonValueKind.Array)
        {
            ParsePages(results, pages);
        }
        else if (root.TryGetProperty("pages", out var legacyPages)
            && legacyPages.ValueKind == JsonValueKind.Array)
        {
            ParsePages(results, legacyPages);
        }

        return results;
    }

    private static void ParsePages(List<RecognizedTextBlock> results, JsonElement pages)
    {
        foreach (var page in pages.EnumerateArray())
        {
            if (!page.TryGetProperty("lines", out var lines) || lines.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var line in lines.EnumerateArray())
            {
                var text = GetString(line, "content") ?? GetString(line, "text");
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                results.Add(CreateTextBlock(text!, line));
            }
        }
    }

    private static RecognizedTextBlock CreateTextBlock(string text, JsonElement line)
    {
        var confidence = GetDecimal(line, "confidence") ?? 0.7m;
        var polygon = GetPolygon(line);
        var isVertical = IsVertical(polygon);

        return new RecognizedTextBlock(
            text,
            confidence,
            polygon.Left,
            polygon.Top,
            polygon.Right,
            polygon.Bottom,
            isVertical);
    }

    private static (int Left, int Top, int Right, int Bottom) GetPolygon(JsonElement line)
    {
        if (!line.TryGetProperty("boundingPolygon", out var polygon) || polygon.ValueKind != JsonValueKind.Array || polygon.GetArrayLength() == 0)
        {
            return (0, 0, 0, 0);
        }

        var points = polygon.EnumerateArray()
            .Select(point => (
                X: (int)Math.Round(GetDecimal(point, "x") ?? 0m),
                Y: (int)Math.Round(GetDecimal(point, "y") ?? 0m)))
            .ToList();

        return (
            points.Min(point => point.X),
            points.Min(point => point.Y),
            points.Max(point => point.X),
            points.Max(point => point.Y));
    }

    private static bool IsVertical((int Left, int Top, int Right, int Bottom) polygon)
    {
        return polygon.Bottom - polygon.Top > polygon.Right - polygon.Left;
    }

    private static string? GetString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static decimal? GetDecimal(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDecimal(out var value) => value,
            JsonValueKind.String when decimal.TryParse(property.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) => value,
            _ => null,
        };
    }
}
