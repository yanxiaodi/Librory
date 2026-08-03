using System.Text;
using System.Text.Json;
using Librory.Application.Recognition;
using Microsoft.Extensions.Options;

namespace Librory.Infrastructure.Recognition;

public sealed class AzureOpenAiVisionFallbackService : IVisionFallbackService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IOptions<RecognitionOptions> _options;

    public AzureOpenAiVisionFallbackService(HttpClient httpClient, IOptions<RecognitionOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<IReadOnlyList<string>> SuggestCandidateTitlesAsync(
        string sourcePhotoPath,
        IReadOnlyList<RecognizedTextBlock> recognizedText,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured(_options.Value.AzureOpenAI))
        {
            throw new InvalidOperationException("Azure OpenAI vision fallback is not configured.");
        }

        var endpoint = NormalizeEndpoint(_options.Value.AzureOpenAI.Endpoint);
        var requestUri = new Uri($"{endpoint}/openai/deployments/{Uri.EscapeDataString(_options.Value.AzureOpenAI.DeploymentName)}/chat/completions?api-version=2024-10-21");

        var prompt = new
        {
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "You extract likely book titles from a shelf or cover photo. Return only a JSON object with a titles array of short title strings."
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = "Suggest likely book titles from the image. Ignore author lines, slogans, and publisher copy. Return up to 10 titles." },
                        new { type = "image_url", image_url = new { url = BuildDataUri(sourcePhotoPath) } }
                    }
                }
            },
            temperature = 0.1,
            max_tokens = 400
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(JsonSerializer.Serialize(prompt, JsonOptions), Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("api-key", _options.Value.AzureOpenAI.ApiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return ParseTitles(content);
    }

    private static bool IsConfigured(AzureOpenAiOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.Endpoint)
            && !string.IsNullOrWhiteSpace(options.ApiKey)
            && !string.IsNullOrWhiteSpace(options.DeploymentName);
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        return endpoint.TrimEnd('/');
    }

    private static string BuildDataUri(string sourcePhotoPath)
    {
        var bytes = File.ReadAllBytes(sourcePhotoPath);
        var extension = Path.GetExtension(sourcePhotoPath).ToLowerInvariant();
        var mimeType = extension switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".heic" => "image/heic",
            ".heif" => "image/heif",
            _ => "image/jpeg",
        };

        return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
    }

    private static IReadOnlyList<string> ParseTitles(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("titles", out var titles) && titles.ValueKind == JsonValueKind.Array)
            {
                return titles
                    .EnumerateArray()
                    .Select(item => item.GetString())
                    .Where(title => !string.IsNullOrWhiteSpace(title))
                    .Select(title => title!.Trim())
                    .ToList();
            }
        }
        catch (JsonException)
        {
        }

        return content
            .Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Trim('-', '*', ' ', '\t'))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Take(10)
            .ToList();
    }
}
