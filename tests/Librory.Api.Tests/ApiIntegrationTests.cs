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

        loginResponse.EnsureSuccessStatusCode();

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
