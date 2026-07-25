using System.Net.Http.Json;
using System.Text.Json;
using Librory.Api.Contracts;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Librory.Application.Wishlist;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Librory.Api.Tests;

public sealed class ApiIntegrationTests
{
    [Fact]
    public async Task DevLogin_is_idempotent_for_same_family_and_member()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var request = new DevLoginRequest("Demo Family", "Test Admin", PreferredLanguage.English);

        var firstResponse = await client.PostAsJsonAsync("/dev/auth/login", request);
        await AssertSuccessAsync(firstResponse);

        var firstLogin = await firstResponse.Content.ReadFromJsonAsync<DevLoginResponse>();
        Assert.NotNull(firstLogin);

        var secondResponse = await client.PostAsJsonAsync("/dev/auth/login", request);
        await AssertSuccessAsync(secondResponse);

        var secondLogin = await secondResponse.Content.ReadFromJsonAsync<DevLoginResponse>();
        Assert.NotNull(secondLogin);

        Assert.Equal(firstLogin!.FamilyId, secondLogin!.FamilyId);
        Assert.Equal(firstLogin.MemberId, secondLogin.MemberId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();

        Assert.Equal(1, await db.Families.CountAsync());
        Assert.Equal(1, await db.Members.CountAsync());
    }

    [Fact]
    public async Task Current_family_endpoint_returns_counts_after_login()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var loginResponse = await client.PostAsJsonAsync(
            "/dev/auth/login",
            new DevLoginRequest("Counts Family", "Counts Admin", PreferredLanguage.English));

        await AssertSuccessAsync(loginResponse);

        var response = await client.GetAsync("/api/family/current");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CurrentFamilyResponse>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.MemberCount);
        Assert.Equal(0, payload.BookCount);
        Assert.Equal(0, payload.WishlistCount);
    }

    [Fact]
    public async Task Bootstrap_creates_a_demo_family_and_is_idempotent()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var firstResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(firstResponse);

        var firstBootstrap = await firstResponse.Content.ReadFromJsonAsync<DevLoginResponse>();
        Assert.NotNull(firstBootstrap);

        var secondResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(secondResponse);

        var secondBootstrap = await secondResponse.Content.ReadFromJsonAsync<DevLoginResponse>();
        Assert.NotNull(secondBootstrap);

        Assert.Equal(firstBootstrap!.FamilyId, secondBootstrap!.FamilyId);
        Assert.Equal(firstBootstrap.MemberId, secondBootstrap.MemberId);
        Assert.Equal("Demo Family", firstBootstrap.FamilyName);
        Assert.Equal("Demo Admin", firstBootstrap.MemberDisplayName);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();

        Assert.Equal(1, await db.Families.CountAsync());
        Assert.Equal(1, await db.Members.CountAsync());
    }

    [Fact]
    public async Task Logout_clears_the_authentication_cookie()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var loginResponse = await client.PostAsJsonAsync(
            "/dev/auth/login",
            new DevLoginRequest("Logout Family", "Logout Admin", PreferredLanguage.English));

        await AssertSuccessAsync(loginResponse);

        var logoutResponse = await client.PostAsync("/dev/auth/logout", content: null);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var response = await client.GetAsync("/api/family/current");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Api_me_route_is_not_mapped()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Wishlist_is_paged()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(bootstrapResponse);

        await client.PostAsJsonAsync("/api/family/current/wishlist", new CreateWishlistItemRequest("Item 1"));
        await client.PostAsJsonAsync("/api/family/current/wishlist", new CreateWishlistItemRequest("Item 2"));
        await client.PostAsJsonAsync("/api/family/current/wishlist", new CreateWishlistItemRequest("Item 3"));

        var response = await client.GetAsync("/api/family/current/wishlist?page=2&pageSize=2");
        await AssertSuccessAsync(response);

        var payload = await response.Content.ReadFromJsonAsync<WishlistPageResponse>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload!.Page);
        Assert.Equal(2, payload.PageSize);
        Assert.Equal(3, payload.TotalCount);
        Assert.Single(payload.Items);
    }

    [Fact]
    public async Task Wishlist_item_created_location_can_be_fetched()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(bootstrapResponse);

        var createResponse = await client.PostAsJsonAsync(
            "/api/family/current/wishlist",
            new CreateWishlistItemRequest("Item 1"));

        await AssertSuccessAsync(createResponse);

        Assert.NotNull(createResponse.Headers.Location);

        var getResponse = await client.GetAsync(createResponse.Headers.Location);
        await AssertSuccessAsync(getResponse);

        var payload = await getResponse.Content.ReadFromJsonAsync<WishlistItemDto>();
        Assert.NotNull(payload);
        Assert.Equal("Item 1", payload!.Title);
    }

    [Fact]
    public async Task Scan_session_can_be_created_and_read_back()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(bootstrapResponse);

        var createResponse = await client.PostAsJsonAsync(
            "/api/family/current/scan-sessions",
            new CreateScanSessionRequest(
                "shelf-photo.jpg",
                3,
                [
                    new CreateScanCandidateRequest(
                        "Charlotte's Web",
                        "High",
                        "E. B. White",
                        0.92m,
                        false,
                        "Already owned by the family"),
                    new CreateScanCandidateRequest(
                        "Matilda",
                        "Medium",
                        "Roald Dahl",
                        0.78m)]));

        await AssertSuccessAsync(createResponse);

        var created = await createResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(created);
        Assert.Equal("shelf-photo.jpg", created!.ShelfPhotoPath);
        Assert.Equal(2, created.Candidates.Count);

        var getResponse = await client.GetAsync($"/api/family/current/scan-sessions/{created.ScanSessionId}");
        await AssertSuccessAsync(getResponse);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(fetched);
        Assert.Equal(created.ScanSessionId, fetched!.ScanSessionId);
        Assert.Equal(created.ShelfPhotoPath, fetched.ShelfPhotoPath);
        Assert.Equal(created.Candidates.Count, fetched.Candidates.Count);
    }

    [Fact]
    public async Task Scan_session_candidate_can_be_corrected_in_place()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(bootstrapResponse);

        var createResponse = await client.PostAsJsonAsync(
            "/api/family/current/scan-sessions",
            new CreateScanSessionRequest(
                "shelf-photo.jpg",
                3,
                [
                    new CreateScanCandidateRequest(
                        "Charlotte's Web",
                        "High",
                        "E. B. White",
                        0.92m,
                        false,
                        "Already owned by the family"),
                    new CreateScanCandidateRequest(
                        "Matilda",
                        "Medium",
                        "Roald Dahl",
                        0.78m)]));

        await AssertSuccessAsync(createResponse);

        var created = await createResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(created);

        var candidateId = created!.Candidates[0].Id;
        var correctionResponse = await client.PutAsJsonAsync(
            $"/api/family/current/scan-sessions/{created.ScanSessionId}/candidates/{candidateId}",
            new UpdateScanCandidateRequest(
                "The Spider and the Pig",
                "Medium",
                "E. B. White",
                0.87m,
                false,
                "Recheck duplicate after correction"));

        await AssertSuccessAsync(correctionResponse);

        var corrected = await correctionResponse.Content.ReadFromJsonAsync<ScanSessionResponse>();
        Assert.NotNull(corrected);
        Assert.Equal(created.ScanSessionId, corrected!.ScanSessionId);
        Assert.Equal(2, corrected.Candidates.Count);

        var correctedCandidate = corrected.Candidates.Single(candidate => candidate.Id == candidateId);
        Assert.Equal("The Spider and the Pig", correctedCandidate.DisplayTitle);
        Assert.Equal("Medium", correctedCandidate.ConfidenceLabel);
        Assert.Equal("E. B. White", correctedCandidate.Author);
        Assert.Equal(0.87m, correctedCandidate.RecommendationScore);
        Assert.False(correctedCandidate.IsAlreadyOwned);
        Assert.Equal("Recheck duplicate after correction", correctedCandidate.DuplicateMessage);

        var untouchedCandidate = corrected.Candidates.Single(candidate => candidate.Id != candidateId);
        Assert.Equal("Matilda", untouchedCandidate.DisplayTitle);
        Assert.Equal("Medium", untouchedCandidate.ConfidenceLabel);
    }

    [Fact]
    public async Task Wishlist_page_validation_omits_empty_error_keys()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        await AssertSuccessAsync(bootstrapResponse);

        var response = await client.GetAsync("/api/family/current/wishlist?page=0&pageSize=0");

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

        using var payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var errors = payload.RootElement.GetProperty("errors");

        Assert.True(errors.TryGetProperty("page", out var pageErrors));
        Assert.True(errors.TryGetProperty("pageSize", out var pageSizeErrors));
        Assert.Equal(JsonValueKind.Array, pageErrors.ValueKind);
        Assert.Equal(JsonValueKind.Array, pageSizeErrors.ValueKind);
        Assert.True(pageErrors.GetArrayLength() > 0);
        Assert.True(pageSizeErrors.GetArrayLength() > 0);
    }

    [Fact]
    public async Task Create_book_work_without_edition_leaves_editions_empty()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var loginResponse = await client.PostAsJsonAsync(
            "/dev/auth/login",
            new DevLoginRequest("Books Family", "Books Admin", PreferredLanguage.English));

        await AssertSuccessAsync(loginResponse);

        var response = await client.PostAsJsonAsync(
            "/api/book-works",
            new CreateBookWorkRequest("The Test Title"));

        await AssertSuccessAsync(response);

        var work = await response.Content.ReadFromJsonAsync<BookWorkResponse>();
        Assert.NotNull(work);
        Assert.Empty(work!.Editions);
    }

    private static async Task AssertSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        Assert.Fail($"Expected success but received {(int)response.StatusCode} {response.StatusCode}.\n{body}");
    }
}
