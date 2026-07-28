using Librory.Api.Endpoints;
using Librory.Application;
using Librory.Application.Families;
using Librory.Application.Scanning;
using Librory.Infrastructure;
using Librory.Infrastructure.Persistence;
using Librory.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddLibroryApplication();
builder.Services.AddLibroryInfrastructure();
builder.Services.AddOptions<ScanSessionOptions>()
    .BindConfiguration("Scanning")
    .Validate(options => options.PhotoRetentionDays > 0 && options.PhotoRetentionDays <= 3650, "Scanning:PhotoRetentionDays must be between 1 and 3650.")
    .Validate(options => options.CleanupIntervalHours > 0 && options.CleanupIntervalHours <= 168, "Scanning:CleanupIntervalHours must be between 1 and 168.")
    .ValidateOnStart();
// ScanStorage defaults to the local repo-root folder and can be overridden per environment.
builder.Services.AddOptions<ScanStorageOptions>()
    .BindConfiguration("ScanStorage")
    .Validate(options => !string.IsNullOrWhiteSpace(options.TemporaryRoot), "ScanStorage:TemporaryRoot is required.")
    .ValidateOnStart();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = ".Librory.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    })
    .AddCookie("External")
    .AddGoogle("Google", options =>
    {
        options.SignInScheme = "External";
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
        options.CallbackPath = "/signin-google";
    })
    .AddMicrosoftAccount("Microsoft", options =>
    {
        options.SignInScheme = "External";
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"] ?? string.Empty;
        options.CallbackPath = "/signin-microsoft";
    });
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
if (builder.Environment.IsDevelopment())
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedHost |
            ForwardedHeaders.XForwardedProto;

        // WARNING: Only safe behind a trusted development tunnel or proxy.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();
}
app.UseAuthentication();
app.UseMiddleware<CurrentFamilyContextMiddleware>();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Librory API");
        options.EnablePersistentAuthentication();
    });
}

app.MapGet("/", () => Results.Ok(new
{
    name = "Librory API",
    status = "running",
    version = "0.2",
})).ExcludeFromDescription();

if (app.Environment.IsDevelopment())
{
    app.MapDevAuthEndpoints();
}

app.MapAuthEndpoints();
app.MapFamilyEndpoints();
app.MapBookWorkEndpoints();
app.MapBookCopyEndpoints();
app.MapRecommendationProfileEndpoints();
app.MapScanSessionEndpoints();
app.MapWishlistEndpoints();

app.MapDefaultEndpoints();

app.Run();

public partial class Program { }
