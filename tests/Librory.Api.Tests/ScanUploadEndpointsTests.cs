using System.Net.Http.Json;
using Librory.Api.Contracts;
using Librory.Application.Scanning;
using Librory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Librory.Api.Tests;

public sealed class ScanUploadEndpointsTests
{
    [Fact]
    public async Task Shelf_photo_upload_creates_a_scan_session()
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

        var response = await client.PostAsync("/api/family/current/scan-sessions/uploads", content);

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created!.ScanSessionId);
        Assert.EndsWith(".jpg", created.ShelfPhotoPath, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(created.Candidates);
        Assert.True(created.ExpiresAt > DateTimeOffset.UtcNow.AddDays(6).AddHours(23));
        Assert.True(created.ExpiresAt <= DateTimeOffset.UtcNow.AddDays(7).AddHours(1));

        Assert.True(File.Exists(created.ShelfPhotoPath));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();
        var hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        var session = await db.ScanSessions.SingleAsync(x => x.Id == created.ScanSessionId);
        Assert.Equal(created.ShelfPhotoPath, session.ShelfPhotoPath);
        Assert.Contains(
            Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, "..", "..", "scan-uploads")),
            created.ShelfPhotoPath,
            StringComparison.OrdinalIgnoreCase);
        Assert.True(session.ExpiresAt > DateTimeOffset.UtcNow.AddDays(6).AddHours(23));
    }

    [Fact]
    public async Task Shelf_photo_upload_rejects_missing_file()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        Assert.True(bootstrapResponse.IsSuccessStatusCode);

        var missingFileResponse = await client.PostAsync(
            "/api/family/current/scan-sessions/uploads",
            new StringContent(string.Empty));

        if (missingFileResponse.StatusCode != System.Net.HttpStatusCode.BadRequest)
        {
            var body = await missingFileResponse.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException(
                $"Missing file expected 400 but received {(int)missingFileResponse.StatusCode} {missingFileResponse.StatusCode}: {body}");
        }
    }

    [Fact]
    public async Task Shelf_photo_upload_rejects_non_image_file()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        Assert.True(bootstrapResponse.IsSuccessStatusCode);

        var invalidContent = new MultipartFormDataContent();
        var textFile = new StringContent("not an image");
        textFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        invalidContent.Add(textFile, "photo", "shelf.txt");

        var invalidFileResponse = await client.PostAsync(
            "/api/family/current/scan-sessions/uploads",
            invalidContent);

        if (invalidFileResponse.StatusCode != System.Net.HttpStatusCode.BadRequest)
        {
            var body = await invalidFileResponse.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException(
                $"Invalid file expected 400 but received {(int)invalidFileResponse.StatusCode} {invalidFileResponse.StatusCode}: {body}");
        }
    }

    [Fact]
    public async Task Shelf_photo_upload_rejects_oversized_file()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        Assert.True(bootstrapResponse.IsSuccessStatusCode);

        var oversizedContent = new MultipartFormDataContent();
        var oversizedImage = new ByteArrayContent(new byte[ScanPhotoUploadPolicy.MaxUploadBytes + 1]);
        oversizedImage.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        oversizedContent.Add(oversizedImage, "photo", "shelf.jpg");

        var oversizedResponse = await client.PostAsync(
            "/api/family/current/scan-sessions/uploads",
            oversizedContent);

        if (oversizedResponse.StatusCode != System.Net.HttpStatusCode.BadRequest)
        {
            var body = await oversizedResponse.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException(
                $"Oversized file expected 400 but received {(int)oversizedResponse.StatusCode} {oversizedResponse.StatusCode}: {body}");
        }
    }

    [Fact]
    public async Task Shelf_photo_upload_uses_configured_retention_window()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Scanning:PhotoRetentionDays"] = "3",
                });
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

        var response = await client.PostAsync("/api/family/current/scan-sessions/uploads", content);

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(created);
        Assert.True(created!.ExpiresAt > DateTimeOffset.UtcNow.AddDays(2).AddHours(23));
        Assert.True(created.ExpiresAt <= DateTimeOffset.UtcNow.AddDays(3).AddHours(1));
    }
}
