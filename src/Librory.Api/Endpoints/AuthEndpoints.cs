using System.Security.Claims;
using Librory.Api.Authentication;
using Librory.Application.Identity;
using Librory.Domain.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Hosting;

namespace Librory.Api.Endpoints;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var auth = app.MapGroup("/auth");

        auth.MapGet("/google/start", async (HttpContext context) => await StartAsync(context, "Google", "/auth/google/callback"))
            .AllowAnonymous()
            .WithTags("Authentication")
            .WithName("AuthGoogleStart");

        auth.MapGet("/google/callback", async (HttpContext context) => await HandleCallbackAsync(context, ExternalIdentityProvider.Google))
            .AllowAnonymous()
            .WithTags("Authentication")
            .WithName("AuthGoogleCallback");

        auth.MapGet("/microsoft/start", async (HttpContext context) => await StartAsync(context, "Microsoft", "/auth/microsoft/callback"))
            .AllowAnonymous()
            .WithTags("Authentication")
            .WithName("AuthMicrosoftStart");

        auth.MapGet("/microsoft/callback", async (HttpContext context) => await HandleCallbackAsync(context, ExternalIdentityProvider.Microsoft))
            .AllowAnonymous()
            .WithTags("Authentication")
            .WithName("AuthMicrosoftCallback");

        auth.MapPost("/logout", async (HttpContext context) => await LogoutAsync(context))
            .AllowAnonymous()
            .WithTags("Authentication")
            .WithName("AuthLogout");

        return app;
    }

    private static async Task StartAsync(HttpContext context, string scheme, string redirectUri)
    {
        await context.ChallengeAsync(scheme, new AuthenticationProperties
        {
            RedirectUri = redirectUri,
        });
    }

    private static async Task HandleCallbackAsync(HttpContext context, ExternalIdentityProvider provider)
    {
        try
        {
            var loginRequest = await ResolveLoginRequestAsync(context, provider);
            if (loginRequest is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var loginService = context.RequestServices.GetRequiredService<IExternalLoginService>();
            var result = await loginService.SignInAsync(loginRequest, context.RequestAborted);

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, AuthenticationSessionFactory.CreatePrincipal(result));
            await context.SignOutAsync("External");

            context.Response.Redirect("/app/home");
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
    }

    private static async Task<ExternalLoginRequest?> ResolveLoginRequestAsync(
        HttpContext context,
        ExternalIdentityProvider provider)
    {
        var external = await context.AuthenticateAsync("External");
        if (external.Succeeded && external.Principal is not null)
        {
            return BuildLoginRequest(provider, external.Principal);
        }

        if (!context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            return null;
        }

        var subject = context.Request.Headers["X-Test-Provider-Subject"].ToString();
        var email = context.Request.Headers["X-Test-Provider-Email"].ToString();
        var displayName = context.Request.Headers["X-Test-Provider-Name"].ToString();
        var preferredLanguageHeader = context.Request.Headers["X-Test-Preferred-Language"].ToString();

        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        var preferredLanguage = Enum.TryParse<PreferredLanguage>(preferredLanguageHeader, ignoreCase: true, out var parsedLanguage)
            ? parsedLanguage
            : PreferredLanguage.English;

        return new ExternalLoginRequest(
            provider,
            subject,
            string.IsNullOrWhiteSpace(email) ? null : email,
            string.IsNullOrWhiteSpace(displayName) ? null : displayName,
            BuildFamilyName(displayName, email),
            BuildMemberDisplayName(displayName, email),
            preferredLanguage);
    }

    private static ExternalLoginRequest? BuildLoginRequest(ExternalIdentityProvider provider, ClaimsPrincipal principal)
    {
        var subject =
            principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
            principal.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        var email = principal.FindFirstValue(ClaimTypes.Email);
        var displayName = principal.FindFirstValue(ClaimTypes.Name);

        return new ExternalLoginRequest(
            provider,
            subject,
            email,
            displayName,
            BuildFamilyName(displayName, email),
            BuildMemberDisplayName(displayName, email),
            PreferredLanguage.English);
    }

    private static string BuildFamilyName(string? displayName, string? email)
    {
        var baseName = BuildMemberDisplayName(displayName, email);
        return $"{baseName} Family";
    }

    private static string BuildMemberDisplayName(string? displayName, string? email)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var localPart = email.Split('@', 2)[0].Trim();
            if (!string.IsNullOrWhiteSpace(localPart))
            {
                return localPart;
            }
        }

        return "Librory Member";
    }

    private static async Task LogoutAsync(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignOutAsync("External");
        context.Response.StatusCode = StatusCodes.Status204NoContent;
    }
}
