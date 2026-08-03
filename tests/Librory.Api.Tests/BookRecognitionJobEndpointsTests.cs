using System.Net;
using System.Net.Http.Json;
using Librory.Api.Contracts;
using Librory.Application.Recognition;
using Librory.Domain.Models;
using Librory.Infrastructure.Recognition;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Librory.Api.Tests;

public sealed class BookRecognitionJobEndpointsTests
{
    [Fact]
    public async Task Posting_a_photo_creates_a_queued_book_recognition_job()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        Assert.True(bootstrapResponse.IsSuccessStatusCode);

        var content = new MultipartFormDataContent();
        var image = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });
        image.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(image, "photo", "shelf.jpg");

        var response = await client.PostAsync("/api/book-recognition-jobs", content);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<BookRecognitionJobResponse>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created!.JobId);
        Assert.Equal(BookRecognitionJobStatus.Queued, created.Status);
        Assert.Empty(created.Candidates);
        Assert.Empty(created.Warnings);
        Assert.Null(created.FailureMessage);
        Assert.EndsWith(".jpg", created.SourcePhotoPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Getting_a_book_recognition_job_returns_the_current_state()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        Assert.True(bootstrapResponse.IsSuccessStatusCode);

        var response = await client.GetAsync("/api/book-recognition-jobs/00000000-0000-0000-0000-000000000001");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Posting_a_photo_rejects_missing_file()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        Assert.True(bootstrapResponse.IsSuccessStatusCode);

        var response = await client.PostAsync("/api/book-recognition-jobs", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Queued_jobs_can_be_processed_into_completed_results()
    {
        await using var factory = await ApiFactory.CreateAsync();
        var pipeline = new FakeRecognitionPipeline();

        using var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBookRecognitionPipeline>();
                services.AddSingleton<IBookRecognitionPipeline>(pipeline);
            });
        });

        using var client = configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        Assert.True(bootstrapResponse.IsSuccessStatusCode);

        var content = new MultipartFormDataContent();
        var image = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });
        image.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(image, "photo", "shelf.jpg");

        var createResponse = await client.PostAsync("/api/book-recognition-jobs", content);
        Assert.Equal(HttpStatusCode.Accepted, createResponse.StatusCode);

        using (var scope = configuredFactory.Services.CreateScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<BookRecognitionJobProcessor>();
            await processor.ProcessQueuedJobsAsync(CancellationToken.None);
        }

        var created = await createResponse.Content.ReadFromJsonAsync<BookRecognitionJobResponse>();
        Assert.NotNull(created);

        var response = await client.GetAsync($"/api/book-recognition-jobs/{created!.JobId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var completed = await response.Content.ReadFromJsonAsync<BookRecognitionJobResponse>();
        Assert.NotNull(completed);
        Assert.Equal(BookRecognitionJobStatus.Succeeded, completed!.Status);
        Assert.Single(completed.Candidates);
        Assert.Single(completed.Candidates[0].MetadataMatches);
    }

    private sealed class FakeRecognitionPipeline : IBookRecognitionPipeline
    {
        public Task<BookRecognitionJobResult> RecognizeAsync(string sourcePhotoPath, string? language, CancellationToken cancellationToken)
        {
            return Task.FromResult(new BookRecognitionJobResult(
                sourcePhotoPath,
                [
                    new BookRecognitionCandidateDto(
                        Guid.NewGuid(),
                        "Dune",
                        "DUNE",
                        940,
                        [
                            new Librory.Application.Metadata.BookMetadataCandidate(
                                "google-books",
                                "source-1",
                                "Dune",
                                null,
                                ["Frank Herbert"],
                                "Ace",
                                "1965",
                                "en",
                                null,
                                "0441013597",
                                "9780441013593",
                                null,
                                null),
                        ]),
                ],
                []));
        }
    }
}
