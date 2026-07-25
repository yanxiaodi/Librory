using System.Net.Http.Json;
using Librory.Api.Contracts;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
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
        using var factory = new ApiFactory();
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
        using var factory = new ApiFactory();
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
        using var factory = new ApiFactory();
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
        using var factory = new ApiFactory();
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
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Wishlist_is_paged()
    {
        using var factory = new ApiFactory();
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
    public async Task Create_book_work_without_edition_leaves_editions_empty()
    {
        using var factory = new ApiFactory();
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
