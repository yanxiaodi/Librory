using System.Net;
using System.Text;
using Librory.Application.Recognition;
using Librory.Infrastructure.Recognition;
using Microsoft.Extensions.Options;
using Xunit;

namespace Librory.Api.Tests;

public sealed class DocumentIntelligenceTextExtractionServiceTests
{
    [Fact]
    public async Task ExtractAsync_sends_document_intelligence_request_and_parses_pages()
    {
        var handler = new RecordingHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "analyzeResult": {
                    "pages": [
                      {
                        "lines": [
                          {
                            "content": "The Hobbit",
                            "confidence": 0.98,
                            "boundingPolygon": [
                              { "x": 1, "y": 2 },
                              { "x": 11, "y": 2 },
                              { "x": 11, "y": 12 },
                              { "x": 1, "y": 12 }
                            ]
                          }
                        ]
                      }
                    ]
                  }
                }
                """, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler);
        var options = Options.Create(new RecognitionOptions
        {
            DocumentIntelligence = new DocumentIntelligenceOptions
            {
                Endpoint = "https://example.cognitiveservices.azure.com/",
                ApiKey = "secret",
            },
        });

        var service = new DocumentIntelligenceTextExtractionService(httpClient, options);
        var tempFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempFile, "image-bytes");

            var blocks = await service.ExtractAsync(tempFile, CancellationToken.None);

            Assert.Single(blocks);
            Assert.Equal("The Hobbit", blocks[0].Text);
            Assert.Equal(0.98m, blocks[0].Confidence);
            Assert.Equal("https://example.cognitiveservices.azure.com/documentintelligence/documentModels/prebuilt-read:analyze?api-version=2024-11-30", handler.RequestUri?.ToString());
            Assert.Equal(HttpMethod.Post, handler.Method);
            Assert.Equal("application/octet-stream", handler.ContentType);
            Assert.Equal("secret", handler.SubscriptionKey);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public RecordingHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public HttpMethod? Method { get; private set; }

        public Uri? RequestUri { get; private set; }

        public string? ContentType { get; private set; }

        public string? SubscriptionKey { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            SubscriptionKey = request.Headers.TryGetValues("Ocp-Apim-Subscription-Key", out var values) ? values.FirstOrDefault() : null;
            return Task.FromResult(_response);
        }
    }
}