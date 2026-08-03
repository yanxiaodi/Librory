using System.Net.Http.Headers;
using System.Globalization;
using System.Text.Json;
using Librory.Application.Recognition;
using Microsoft.Extensions.Options;

namespace Librory.Infrastructure.Recognition;

public sealed class AzureAiVisionTextExtractionService : IOcrTextExtractionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IOptions<RecognitionOptions> _options;

    public AzureAiVisionTextExtractionService(HttpClient httpClient, IOptions<RecognitionOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<IReadOnlyList<RecognizedTextBlock>> ExtractAsync(
        string sourcePhotoPath,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured(_options.Value.AzureVision))
        {
            throw new InvalidOperationException("Azure Vision OCR is not configured.");
        }

        var endpoint = NormalizeEndpoint(_options.Value.AzureVision.Endpoint);
        var requestUri = new Uri($"{endpoint}/computervision/imageanalysis:analyze?api-version=2024-02-01&features=read");

        await using var stream = File.OpenRead(sourcePhotoPath);
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StreamContent(stream),
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        request.Headers.Add("Ocp-Apim-Subscription-Key", _options.Value.AzureVision.ApiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        return ParseRecognizedTextBlocks(document.RootElement);
    }

    private static bool IsConfigured(AzureVisionOptions options)
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

        if (root.TryGetProperty("readResult", out var readResult)
            && readResult.TryGetProperty("blocks", out var blocks)
            && blocks.ValueKind == JsonValueKind.Array)
        {
            ParseBlocks(results, blocks);
        }
        else if (root.TryGetProperty("analyzeResult", out var analyzeResult)
            && analyzeResult.TryGetProperty("readResults", out var readResults)
            && readResults.ValueKind == JsonValueKind.Array)
        {
            ParseReadResults(results, readResults);
        }
        else if (root.TryGetProperty("readResults", out var legacyReadResults)
            && legacyReadResults.ValueKind == JsonValueKind.Array)
        {
            ParseReadResults(results, legacyReadResults);
        }

        return results;
    }

    private static void ParseBlocks(List<RecognizedTextBlock> results, JsonElement blocks)
    {
        foreach (var block in blocks.EnumerateArray())
        {
            if (!block.TryGetProperty("lines", out var lines) || lines.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var line in lines.EnumerateArray())
            {
                var text = GetString(line, "text") ?? GetString(line, "content");
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                results.Add(CreateTextBlock(text!, line));
            }
        }
    }

    private static void ParseReadResults(List<RecognizedTextBlock> results, JsonElement readResults)
    {
        foreach (var page in readResults.EnumerateArray())
        {
            if (!page.TryGetProperty("lines", out var lines) || lines.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var line in lines.EnumerateArray())
            {
                var text = GetString(line, "text") ?? GetString(line, "content");
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
