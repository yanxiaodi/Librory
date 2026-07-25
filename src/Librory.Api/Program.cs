using Librory.Api.Endpoints;
using Librory.Application;
using Librory.Application.Families;
using Librory.Infrastructure;
using Librory.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddLibroryApplication();
builder.Services.AddLibroryInfrastructure();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".Librory.DevAuth";
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
    });
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

var app = builder.Build();

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

app.MapDevAuthEndpoints();
app.MapFamilyEndpoints();
app.MapBookWorkEndpoints();
app.MapWishlistEndpoints();

app.MapDefaultEndpoints();

app.Run();
