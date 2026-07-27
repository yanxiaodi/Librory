# Story 01 Backend Login Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the Koviva-style backend login slice so Librory can authenticate with Google and Microsoft, persist external identities, bootstrap a singleton family on first login, and issue the cookie session used by the rest of the API.

**Architecture:** Keep the domain model as the source of truth for `Family`, `Member`, and `ExternalIdentity`, add EF Core persistence for linked identities, introduce an application-level external login service that resolves or bootstraps a member from a provider identity, and let the API host translate provider sign-ins into the existing cookie session and current-family claims. Keep `/dev/auth/*` available so local development remains frictionless while the real login path lands.

**Tech Stack:** C# / .NET 10, ASP.NET Core authentication, EF Core 10, PostgreSQL, xUnit, Microsoft.AspNetCore.Mvc.Testing, Microsoft.AspNetCore.Authentication cookies and provider handlers.

## Global Constraints

- External identities are keyed by provider plus provider subject.
- The first login must create a singleton family and initial admin member when no linked identity exists.
- Cookie auth remains the API session contract.
- `CurrentFamilyContextMiddleware` must continue to resolve the family/member claims from the signed-in cookie.
- Dev auth must remain available for local development.
- The backend should expose Google and Microsoft sign-in outcomes at the API boundary.

---

### Task 1: Persist linked external identities in PostgreSQL

**Files:**
- Modify: `src/Librory.Infrastructure/Persistence/Configurations/MemberConfiguration.cs`
- Modify: `tests/Librory.Api.Tests/LibroryDbContextModelTests.cs`
- Modify: `src/Librory.Infrastructure/Persistence/LibroryDbContextModelSnapshot.cs`
- Create: a new EF migration under `src/Librory.Infrastructure/Persistence/Migrations/`

**Interfaces:**
- Consumes: `Member.ExternalIdentities`, `ExternalIdentity`, `ExternalIdentityProvider`
- Produces: a persisted `member_external_identities` collection with a unique `(provider, provider_subject)` constraint

- [ ] **Step 1: Write the failing model test**

```csharp
[Fact]
public void Model_persists_member_external_identities()
{
    var options = new DbContextOptionsBuilder<LibroryDbContext>()
        .UseInMemoryDatabase(nameof(Model_persists_member_external_identities))
        .Options;

    using var db = new LibroryDbContext(options);

    var memberType = db.Model.FindEntityType(typeof(Member));
    Assert.NotNull(memberType);

    var ownedIdentityType = db.Model.GetEntityTypes()
        .Single(entity => entity.GetTableName() == "member_external_identities");

    Assert.Contains(ownedIdentityType.GetIndexes(), index =>
        index.IsUnique &&
        index.Properties.Select(property => property.Name).SequenceEqual(["Provider", "ProviderSubject"]));
}
```

- [ ] **Step 2: Run the test to confirm the mapping does not exist yet**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --filter FullyQualifiedName~LibroryDbContextModelTests`
Expected: fail because `member_external_identities` is not mapped yet.

- [ ] **Step 3: Implement the owned collection mapping**

```csharp
builder.OwnsMany(x => x.ExternalIdentities, owned =>
{
    owned.ToTable("member_external_identities");
    owned.WithOwner().HasForeignKey("MemberId");
    owned.Property(identity => identity.Provider).HasConversion<string>().HasMaxLength(32);
    owned.Property(identity => identity.ProviderSubject).HasMaxLength(200);
    owned.Property(identity => identity.Email).HasMaxLength(256);
    owned.Property(identity => identity.DisplayName).HasMaxLength(200);
    owned.Property(identity => identity.LinkedAt);
    owned.HasKey("MemberId", "Provider", "ProviderSubject");
    owned.HasIndex(identity => new { identity.Provider, identity.ProviderSubject }).IsUnique();
});
```

- [ ] **Step 4: Generate and apply the migration**

Run: `dotnet ef migrations add AddMemberExternalIdentities --project src/Librory.Infrastructure --startup-project src/Librory.Api`

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --filter FullyQualifiedName~LibroryDbContextModelTests`
Expected: pass, with the owned collection mapped and the uniqueness constraint in place.

- [ ] **Step 5: Commit the persistence layer**

```bash
git add src/Librory.Infrastructure/Persistence/Configurations/MemberConfiguration.cs src/Librory.Infrastructure/Persistence/Migrations src/Librory.Infrastructure/Persistence/LibroryDbContextModelSnapshot.cs tests/Librory.Api.Tests/LibroryDbContextModelTests.cs
git commit -m "feat: persist external identities"
```

### Task 2: Add the application login coordinator

**Files:**
- Create: `src/Librory.Application/Identity/ExternalLoginRequest.cs`
- Create: `src/Librory.Application/Identity/ExternalLoginResult.cs`
- Create: `src/Librory.Application/Identity/IExternalLoginService.cs`
- Modify: `src/Librory.Application/DependencyInjection.cs`
- Create: `src/Librory.Infrastructure/Identity/ExternalLoginService.cs`
- Modify: `src/Librory.Infrastructure/DependencyInjection.cs`
- Create: `tests/Librory.Api.Tests/ExternalLoginServiceTests.cs`

**Interfaces:**
- `public sealed record ExternalLoginRequest(ExternalIdentityProvider Provider, string ProviderSubject, string? Email, string? DisplayName, string SuggestedFamilyName, string SuggestedMemberDisplayName, PreferredLanguage PreferredLanguage)`
- `public sealed record ExternalLoginResult(Guid FamilyId, string FamilyName, Guid MemberId, string MemberDisplayName, MemberRole MemberRole, PreferredLanguage PreferredLanguage, bool IsNewMember)`
- `public interface IExternalLoginService { Task<ExternalLoginResult> SignInAsync(ExternalLoginRequest request, CancellationToken cancellationToken); }`
- Consumes: `LibroryDbContext`, `FirstLoginFamilyBootstrapper`, `ExternalIdentityResolver`
- Produces: persisted family/member rows, linked external identities, and a signed-in result object for the API layer

- [ ] **Step 1: Write service tests for first login and repeated login**

```csharp
[Fact]
public async Task SignInAsync_bootstraps_a_family_on_first_login()
{
    await using var factory = await ApiFactory.CreateAsync();
    using var scope = factory.Services.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<IExternalLoginService>();

    var result = await service.SignInAsync(
        new ExternalLoginRequest(
            ExternalIdentityProvider.Google,
            "google-subject-123",
            "alice@example.com",
            "Alice",
            "Alice Family",
            "Alice",
            PreferredLanguage.English),
        CancellationToken.None);

    Assert.True(result.IsNewMember);
    Assert.Equal("Alice Family", result.FamilyName);
    Assert.Equal("Alice", result.MemberDisplayName);
}
```

```csharp
[Fact]
public async Task SignInAsync_reuses_an_existing_member_for_the_same_provider_subject()
{
    await using var factory = await ApiFactory.CreateAsync();
    using var scope = factory.Services.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<IExternalLoginService>();

    var first = await service.SignInAsync(
        new ExternalLoginRequest(
            ExternalIdentityProvider.Google,
            "google-subject-123",
            "alice@example.com",
            "Alice",
            "Alice Family",
            "Alice",
            PreferredLanguage.English),
        CancellationToken.None);

    var second = await service.SignInAsync(
        new ExternalLoginRequest(
            ExternalIdentityProvider.Google,
            "google-subject-123",
            "alice@example.com",
            "Alice",
            "Alice Family",
            "Alice",
            PreferredLanguage.English),
        CancellationToken.None);

    Assert.Equal(first.FamilyId, second.FamilyId);
    Assert.Equal(first.MemberId, second.MemberId);
}
```

- [ ] **Step 2: Run the service tests to confirm the coordinator does not exist yet**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --filter FullyQualifiedName~ExternalLoginServiceTests`
Expected: fail because the application login service is not implemented yet.

- [ ] **Step 3: Implement the login coordinator**

```csharp
public async Task<ExternalLoginResult> SignInAsync(ExternalLoginRequest request, CancellationToken cancellationToken)
{
    var member = await _db.Members
        .Include(x => x.ExternalIdentities)
        .Include(x => x.Family)
        .SingleOrDefaultAsync(x => x.ExternalIdentities.Any(identity =>
            identity.Provider == request.Provider &&
            identity.ProviderSubject == request.ProviderSubject), cancellationToken);

    if (member is null)
    {
        var bootstrap = FirstLoginFamilyBootstrapper.Bootstrap(
            request.SuggestedFamilyName,
            request.SuggestedMemberDisplayName,
            new ExternalIdentity(request.Provider, request.ProviderSubject, request.Email, request.DisplayName, DateTimeOffset.UtcNow),
            request.PreferredLanguage);

        _db.Families.Add(bootstrap.Family);
        await _db.SaveChangesAsync(cancellationToken);
        return new ExternalLoginResult(
            bootstrap.Family.Id,
            bootstrap.Family.Name,
            bootstrap.InitialMember.Id,
            bootstrap.InitialMember.DisplayName,
            bootstrap.InitialMember.Role,
            bootstrap.InitialMember.PreferredLanguage,
            true);
    }

    return new ExternalLoginResult(
        member.FamilyId,
        member.Family.Name,
        member.Id,
        member.DisplayName,
        member.Role,
        member.PreferredLanguage,
        false);
}
```

- [ ] **Step 4: Run the service tests again and verify the login coordinator works**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --filter FullyQualifiedName~ExternalLoginServiceTests`
Expected: pass, with first-login bootstrap and identity reuse both covered.

- [ ] **Step 5: Commit the application service**

```bash
git add src/Librory.Application/Identity src/Librory.Application/DependencyInjection.cs src/Librory.Infrastructure/Identity src/Librory.Infrastructure/DependencyInjection.cs tests/Librory.Api.Tests/ExternalLoginServiceTests.cs
git commit -m "feat: add external login coordinator"
```

### Task 3: Wire real auth schemes and API endpoints

**Files:**
- Modify: `src/Librory.Api/Program.cs`
- Modify: `src/Librory.Api/Librory.Api.csproj`
- Create: `src/Librory.Api/Endpoints/AuthEndpoints.cs`
- Create: `src/Librory.Api/Authentication/AuthenticationSessionFactory.cs`
- Modify: `src/Librory.Api/Endpoints/DevAuthEndpoints.cs`
- Create: `tests/Librory.Api.Tests/TestExternalAuthHandler.cs`
- Create: `tests/Librory.Api.Tests/AuthEndpointsTests.cs`
- Modify: `tests/Librory.Api.Tests/ApiFactory.cs`

**Interfaces:**
- `GET /auth/google/start`
- `GET /auth/google/callback`
- `GET /auth/microsoft/start`
- `GET /auth/microsoft/callback`
- `POST /auth/logout`
- `AuthenticationSessionFactory.CreatePrincipal(ExternalLoginResult result)` builds the app cookie claims
- Consumes: the login coordinator from Task 2
- Produces: a signed app cookie with `family id`, `member id`, `member role`, and `preferred language`

- [ ] **Step 1: Write endpoint tests around a fake external provider**

```csharp
[Fact]
public async Task Google_callback_issues_the_app_cookie_and_redirects_home()
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
```

- [ ] **Step 2: Run the endpoint tests to confirm the auth routes do not exist yet**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --filter FullyQualifiedName~AuthEndpointsTests`
Expected: fail because the real auth schemes and endpoints are not wired yet.

- [ ] **Step 3: Implement cookie auth plus Google and Microsoft sign-in routes**

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = ".Librory.Auth";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    })
    .AddCookie("External")
    .AddGoogle("Google", options =>
    {
        options.SignInScheme = "External";
        options.CallbackPath = "/signin-google";
    })
    .AddMicrosoftAccount("Microsoft", options =>
    {
        options.SignInScheme = "External";
        options.CallbackPath = "/signin-microsoft";
    });

app.MapGet("/auth/google/start", async (HttpContext context) =>
{
    await context.ChallengeAsync("Google", new AuthenticationProperties { RedirectUri = "/auth/google/callback" });
});
```

```csharp
app.MapGet("/auth/google/callback", async (
    HttpContext context,
    IExternalLoginService loginService) =>
{
    var external = await context.AuthenticateAsync("External");
    if (!external.Succeeded) return Results.Unauthorized();

    var result = await loginService.SignInAsync(
        new ExternalLoginRequest(
            ExternalIdentityProvider.Google,
            external.Principal!.FindFirstValue(ClaimTypes.NameIdentifier)!,
            external.Principal.FindFirstValue(ClaimTypes.Email),
            external.Principal.FindFirstValue(ClaimTypes.Name),
            external.Principal.FindFirstValue(ClaimTypes.Name) ?? "Librory Family",
            external.Principal.FindFirstValue(ClaimTypes.Name) ?? "Librory Member",
            PreferredLanguage.English),
        context.RequestAborted);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, AuthenticationSessionFactory.CreatePrincipal(result));
    return Results.Redirect("/app/home");
});
```

```csharp
app.MapPost("/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync("External");
    return Results.NoContent();
});
```

- [ ] **Step 4: Run the endpoint tests again and verify the cookie flow works**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --filter FullyQualifiedName~AuthEndpointsTests`
Expected: pass, with both provider routes issuing the app cookie and logout clearing it.

- [ ] **Step 5: Commit the auth wiring**

```bash
git add src/Librory.Api/Program.cs src/Librory.Api/Librory.Api.csproj src/Librory.Api/Endpoints src/Librory.Api/Authentication tests/Librory.Api.Tests/ApiFactory.cs tests/Librory.Api.Tests/TestExternalAuthHandler.cs tests/Librory.Api.Tests/AuthEndpointsTests.cs tests/Librory.Api.Tests/ExternalLoginServiceTests.cs
git commit -m "feat: wire external auth routes"
```

### Task 4: Update docs and verify the end-to-end backend slice

**Files:**
- Update: `docs/backend-story-map.md`
- Update: `docs/api-reference.md`
- Update: `docs/frontend-integration-guide.md`
- Add: `docs/devlog/2026-07-26-story-01-backend-login.md`

**Interfaces:**
- Documents the real auth routes and the preserved dev auth routes
- Documents that the post-login session is cookie-based and resolves `/api/family/current`

- [ ] **Step 1: Update the API and frontend integration docs**

```md
Sign-in:

- `GET /auth/google/start`
- `GET /auth/google/callback`
- `GET /auth/microsoft/start`
- `GET /auth/microsoft/callback`

Development:

- `POST /dev/auth/login`
- `POST /dev/bootstrap`
- `POST /dev/auth/logout`
```

- [ ] **Step 2: Add a devlog note for the backend login slice**

```md
# 2026-07-26 Story 01 Backend Login

- Persisted linked external identities for provider-based sign-in.
- Added the login coordinator that bootstraps a singleton family on first login.
- Wired Google and Microsoft auth routes to the existing cookie session contract.
- Kept dev auth available for local debugging.
```

- [ ] **Step 3: Run the focused backend test suite**

Run: `dotnet test tests/Librory.Domain.Tests/Librory.Domain.Tests.csproj`

Run: `dotnet test tests/Librory.Application.Tests/Librory.Application.Tests.csproj`

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj`

Expected: pass, with the new login flow and the existing family/current auth behavior both green.

- [ ] **Step 4: Run the API build**

Run: `dotnet build src/Librory.Api/Librory.Api.csproj`
Expected: pass.

- [ ] **Step 5: Commit the docs and verification updates**

```bash
git add docs/backend-story-map.md docs/api-reference.md docs/frontend-integration-guide.md docs/devlog/2026-07-26-story-01-backend-login.md
git commit -m "docs: update backend login flow"
```
