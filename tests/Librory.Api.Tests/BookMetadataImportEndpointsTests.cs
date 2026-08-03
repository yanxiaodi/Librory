using System.Net;
using System.Net.Http.Json;
using Librory.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Librory.Api.Tests;

public sealed class BookMetadataImportEndpointsTests
{
    [Fact]
    public async Task Posting_a_normalized_candidate_imports_a_canonical_book_work()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        Assert.True(bootstrapResponse.IsSuccessStatusCode);

        var response = await client.PostAsJsonAsync("/api/book-metadata/import", new BookMetadataImportRequest(
            new BookMetadataImportCandidateRequest(
                "GoogleBooks",
                "volume-1",
                "Dune",
                null,
                ["Frank Herbert"],
                "Ace",
                "1965",
                "en",
                "A science fiction novel.",
                "0441013597",
                "9780441013593",
                null,
                null)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<BookWorkResponse>();
        Assert.NotNull(created);
        Assert.Equal("Dune", created!.Title);
        Assert.Equal("Frank Herbert", created.Author);
        Assert.Single(created.Editions);
        Assert.Equal("9780441013593", created.Editions[0].Isbn);
    }

    [Fact]
    public async Task Posting_a_candidate_with_an_existing_isbn_reuses_the_existing_canonical_work()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        Assert.True(bootstrapResponse.IsSuccessStatusCode);

        var createdWorkResponse = await client.PostAsJsonAsync("/api/book-works", new CreateBookWorkRequest(
            "Dune",
            "Frank Herbert",
            "9780441013593",
            "Paperback",
            1965));

        Assert.Equal(HttpStatusCode.Created, createdWorkResponse.StatusCode);

        var createdWork = await createdWorkResponse.Content.ReadFromJsonAsync<BookWorkResponse>();
        Assert.NotNull(createdWork);

        var importResponse = await client.PostAsJsonAsync("/api/book-metadata/import", new BookMetadataImportRequest(
            new BookMetadataImportCandidateRequest(
                "GoogleBooks",
                "volume-1",
                "Dune",
                null,
                ["Frank Herbert"],
                "Ace",
                "1965",
                "en",
                "A science fiction novel.",
                "0441013597",
                "9780441013593",
                null,
                null)));

        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        var imported = await importResponse.Content.ReadFromJsonAsync<BookWorkResponse>();
        Assert.NotNull(imported);
        Assert.Equal(createdWork!.BookWorkId, imported!.BookWorkId);
        Assert.Single(imported.Editions);
        Assert.Equal(createdWork.Editions[0].Isbn, imported.Editions[0].Isbn);
    }

    [Fact]
    public async Task Posting_a_candidate_with_a_blank_title_returns_validation_problem()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        Assert.True(bootstrapResponse.IsSuccessStatusCode);

        var response = await client.PostAsJsonAsync("/api/book-metadata/import", new BookMetadataImportRequest(
            new BookMetadataImportCandidateRequest(
                "GoogleBooks",
                "volume-1",
                "   ",
                null,
                [],
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
