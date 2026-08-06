using System.Net;
using Librory.Domain.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Librory.Api.Tests;

public sealed class AuthEndpointsTests
{
    [Fact]
    public async Task Google_login_redirects_and_issues_the_app_cookie()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });

        var start = await client.GetAsync("/auth/google/start");
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);

        var callbackRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/google/callback");
        callbackRequest.Headers.Add("X-Test-Provider-Subject", "google-subject-123");
        callbackRequest.Headers.Add("X-Test-Provider-Email", "alice@example.com");
        callbackRequest.Headers.Add("X-Test-Provider-Name", "Alice");

        var callback = await client.SendAsync(callbackRequest);
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.EndsWith("/app/home", callback.Headers.Location!.ToString());

        var current = await client.GetAsync("/api/family/current");
        current.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Microsoft_login_redirects_and_issues_the_app_cookie()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });

        var start = await client.GetAsync("/auth/microsoft/start");
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);

        var callbackRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/microsoft/callback");
        callbackRequest.Headers.Add("X-Test-Provider-Subject", "microsoft-subject-456");
        callbackRequest.Headers.Add("X-Test-Provider-Email", "bob@example.com");
        callbackRequest.Headers.Add("X-Test-Provider-Name", "Bob");

        var callback = await client.SendAsync(callbackRequest);
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.EndsWith("/app/home", callback.Headers.Location!.ToString());

        var current = await client.GetAsync("/api/family/current");
        current.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Logout_clears_the_app_auth_cookie()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });

        var callbackRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/google/callback");
        callbackRequest.Headers.Add("X-Test-Provider-Subject", "google-subject-789");
        callbackRequest.Headers.Add("X-Test-Provider-Email", "carol@example.com");
        callbackRequest.Headers.Add("X-Test-Provider-Name", "Carol");

        var callback = await client.SendAsync(callbackRequest);
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);

        var logout = await client.PostAsync("/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var response = await client.GetAsync("/api/family/current");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Callback_without_provider_subject_returns_unauthorized()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });

        var callbackRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/google/callback");
        callbackRequest.Headers.Add("X-Test-Provider-Email", "alice@example.com");
        callbackRequest.Headers.Add("X-Test-Provider-Name", "Alice");

        var callback = await client.SendAsync(callbackRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, callback.StatusCode);
    }

    [Fact]
    public async Task Callback_preserves_a_safe_local_return_url()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var callbackRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/google/callback?returnUrl=%2Ffamily-invitations%2Ftoken");
        callbackRequest.Headers.Add("X-Test-Provider-Subject", "google-subject-return-url");
        callbackRequest.Headers.Add("X-Test-Provider-Email", "invitee@example.com");
        callbackRequest.Headers.Add("X-Test-Provider-Name", "Invitee");

        var callback = await client.SendAsync(callbackRequest);

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal("/family-invitations/token", callback.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Callback_rejects_an_external_return_url()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var callbackRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/google/callback?returnUrl=https%3A%2F%2Fevil.example");
        callbackRequest.Headers.Add("X-Test-Provider-Subject", "google-subject-unsafe-url");
        callbackRequest.Headers.Add("X-Test-Provider-Email", "invitee@example.com");
        callbackRequest.Headers.Add("X-Test-Provider-Name", "Invitee");

        var callback = await client.SendAsync(callbackRequest);

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal("/app/home", callback.Headers.Location!.ToString());
    }
}
