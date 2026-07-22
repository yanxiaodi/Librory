# Librory MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first end-to-end Librory slice: scan a shelf, rank candidate books, flag duplicates, correct mistakes, and save books into a family-aware library model.

**Architecture:** Keep the first pass thin and vertical. The API owns orchestration, the Infrastructure project provides in-memory services and seeded catalog data, and the Web app renders a shelf-review workflow that can be exercised without waiting for the full persistence layer. Preserve the existing Work -> Edition -> Copy model and add only the minimum extra contracts needed for recommendations, duplicate warnings, family roles, and wishlist intake.

**Tech Stack:** ASP.NET Core Minimal APIs, C#, .NET Aspire AppHost, React 19, Vite, TypeScript, Tailwind CSS, Radix UI, shadcn/ui-style composition, xUnit, in-memory fakes for the first slice.

---

## File Structure

- `src/Librory.Domain/Models/*` holds persistent domain primitives.
- `src/Librory.Application/Scanning/*`, `Families/*`, `Wishlist/*`, and `Intake/*` hold use-case contracts and DTOs.
- `src/Librory.Infrastructure/Scanning/*` and `src/Librory.Infrastructure/Wishlist/*` hold the first in-memory implementations.
- `src/Librory.Api/Endpoints/*` holds minimal API route modules.
- `src/Librory.Web/src/features/*` and `src/Librory.Web/src/lib/libroryApi.ts` hold the route-level UI and API client.
- `tests/Librory.*.Tests/*` holds contract and endpoint tests.

## Delivery Phases

- Phase 1: Shelf scan, recommendation, duplicate warning, and correction loop.
- Phase 2: Family library intake, wishlist, and basic browse screens.
- Phase 3: Swap in persistence and external book metadata providers.

---

### Task 1: Define the shared contracts and domain primitives

**Files:**
- Create: `src/Librory.Domain/Models/MemberRole.cs`
- Create: `src/Librory.Domain/Models/RecommendationProfile.cs`
- Create: `src/Librory.Domain/Models/WishlistItem.cs`
- Modify: `src/Librory.Domain/Models/Family.cs`
- Modify: `src/Librory.Domain/Models/Member.cs`
- Modify: `src/Librory.Domain/Models/BookCopy.cs`
- Create: `src/Librory.Application/Scanning/ScanShelfRequest.cs`
- Create: `src/Librory.Application/Scanning/ScanCandidateDto.cs`
- Create: `src/Librory.Application/Scanning/ScanSessionDto.cs`
- Create: `src/Librory.Application/Scanning/CorrectionRequest.cs`
- Create: `src/Librory.Application/Scanning/IScanSessionService.cs`
- Create: `src/Librory.Application/Recommendations/RecommendationProfileDto.cs`
- Create: `src/Librory.Application/Wishlist/WishlistItemDto.cs`
- Create: `src/Librory.Application/Families/FamilySummaryDto.cs`
- Modify: `src/Librory.Application/DependencyInjection.cs`
- Create: `tests/Librory.Domain.Tests/Librory.Domain.Tests.csproj`
- Create: `tests/Librory.Application.Tests/Librory.Application.Tests.csproj`

- [ ] **Step 1: Add the failing contract tests**

Create `tests/Librory.Domain.Tests/MemberRoleTests.cs`:

```csharp
using Librory.Domain.Models;

namespace Librory.Domain.Tests;

public class MemberRoleTests
{
    [Fact]
    public void MemberRole_includes_admin_and_member()
    {
        Assert.Contains(MemberRole.Admin, Enum.GetValues<MemberRole>());
        Assert.Contains(MemberRole.Member, Enum.GetValues<MemberRole>());
    }
}
```

Create `tests/Librory.Application.Tests/ScanContractTests.cs`:

```csharp
using Librory.Application.Scanning;

namespace Librory.Application.Tests;

public class ScanContractTests
{
    [Fact]
    public void ScanShelfRequest_has_family_and_language_fields()
    {
        var request = new ScanShelfRequest(Guid.NewGuid(), "zh", "shelf-photo.jpg");

        Assert.Equal("zh", request.PreferredLanguage);
        Assert.Equal("shelf-photo.jpg", request.ShelfPhotoPath);
    }
}
```

- [ ] **Step 2: Run the tests to confirm the contracts are missing**

Run:

```bash
dotnet test tests/Librory.Domain.Tests/Librory.Domain.Tests.csproj -v minimal
dotnet test tests/Librory.Application.Tests/Librory.Application.Tests.csproj -v minimal
```

Expected: fail because the new files and projects do not exist yet.

- [ ] **Step 3: Add the minimum domain and DTO types**

Use these shapes as the starting point:

```csharp
namespace Librory.Domain.Models;

public enum MemberRole
{
    Member = 0,
    Admin = 1
}

public sealed class RecommendationProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid MemberId { get; set; }
    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }
    public List<string> FavoriteAuthors { get; } = [];
    public List<string> FavoriteGenres { get; } = [];
    public List<string> FavoriteStyles { get; } = [];
}
```

```csharp
namespace Librory.Application.Scanning;

public sealed record ScanShelfRequest(
    Guid FamilyId,
    string? PreferredLanguage,
    string ShelfPhotoPath);

public sealed record ScanCandidateDto(
    Guid CandidateId,
    string DisplayTitle,
    string? Author,
    decimal RecommendationScore,
    bool IsAlreadyOwned,
    string? DuplicateMessage,
    string ConfidenceLabel);

public sealed record ScanSessionDto(
    Guid ScanSessionId,
    Guid FamilyId,
    IReadOnlyList<ScanCandidateDto> Candidates,
    DateTimeOffset ExpiresAt);

public sealed record CorrectionRequest(
    string? RescannedPhotoPath,
    string? ManualTitle);

public interface IScanSessionService
{
    Task<ScanSessionDto> StartShelfScanAsync(ScanShelfRequest request, CancellationToken cancellationToken);
    Task<ScanSessionDto> ApplyCorrectionAsync(Guid scanSessionId, Guid candidateId, CorrectionRequest request, CancellationToken cancellationToken);
}

public sealed record RecommendationProfileDto(
    Guid MemberId,
    int? MinimumAge,
    int? MaximumAge,
    IReadOnlyList<string> FavoriteAuthors,
    IReadOnlyList<string> FavoriteGenres,
    IReadOnlyList<string> FavoriteStyles);

public sealed record WishlistItemDto(
    Guid WishlistItemId,
    Guid? BookWorkId,
    string Title,
    string? Author);

public sealed record FamilySummaryDto(
    Guid FamilyId,
    string FamilyName,
    IReadOnlyList<string> MemberNames,
    int BookCount);
```

- [ ] **Step 4: Run `dotnet test` and `dotnet build` again**

Run:

```bash
dotnet test tests/Librory.Domain.Tests/Librory.Domain.Tests.csproj -v minimal
dotnet test tests/Librory.Application.Tests/Librory.Application.Tests.csproj -v minimal
dotnet build Librory.sln
```

Expected: the contract tests pass, and the solution still builds.

- [ ] **Step 5: Commit the contract layer**

```bash
git add src/Librory.Domain/Models src/Librory.Application tests
git commit -m "Add scan and recommendation contracts"
```

---

### Task 2: Implement the first in-memory shelf scan workflow

**Files:**
- Create: `src/Librory.Infrastructure/Scanning/InMemoryScanSessionService.cs`
- Create: `src/Librory.Infrastructure/Scanning/StaticBookCatalog.cs`
- Create: `src/Librory.Infrastructure/Scanning/RuleBasedRecommendationEngine.cs`
- Create: `src/Librory.Infrastructure/Wishlist/InMemoryWishlistStore.cs`
- Modify: `src/Librory.Infrastructure/DependencyInjection.cs`
- Modify: `tests/Librory.Application.Tests/Librory.Application.Tests.csproj`

- [ ] **Step 1: Add a failing workflow test**

Create `tests/Librory.Application.Tests/ScanRankingTests.cs`:

```csharp
using Librory.Application.Scanning;

namespace Librory.Application.Tests;

public class ScanRankingTests
{
    [Fact]
    public async Task Scan_results_are_ranked_with_duplicates_flagged()
    {
        var service = new InMemoryScanSessionService(new StaticBookCatalog(), new RuleBasedRecommendationEngine());
        var result = await service.StartShelfScanAsync(
            new ScanShelfRequest(Guid.NewGuid(), "en", "shelf.jpg"),
            CancellationToken.None);

        Assert.NotEmpty(result.Candidates);
        Assert.Contains(result.Candidates, candidate => candidate.IsAlreadyOwned);
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

Run:

```bash
dotnet test tests/Librory.Application.Tests/Librory.Application.Tests.csproj -v minimal --filter ScanRankingTests
```

Expected: fail because `InMemoryScanSessionService`, `StaticBookCatalog`, and `RuleBasedRecommendationEngine` do not exist yet.

- [ ] **Step 3: Add the in-memory implementation**

Use these public shapes:

```csharp
namespace Librory.Infrastructure.Scanning;

public sealed class InMemoryScanSessionService : IScanSessionService
{
    public InMemoryScanSessionService(
        StaticBookCatalog catalog,
        RuleBasedRecommendationEngine recommendationEngine)
    {
        // keep the constructor explicit so the dependencies stay visible
    }

    public Task<ScanSessionDto> StartShelfScanAsync(ScanShelfRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<ScanSessionDto> ApplyCorrectionAsync(Guid scanSessionId, Guid candidateId, CorrectionRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
```

Seed the catalog with a small fixed list of recognizable books so the first UI can demonstrate ranking, duplicate detection, and correction without waiting for real metadata providers.

- [ ] **Step 4: Run the workflow test and the full build**

Run:

```bash
dotnet test tests/Librory.Application.Tests/Librory.Application.Tests.csproj -v minimal --filter ScanRankingTests
dotnet build Librory.sln
```

Expected: the workflow test passes and the solution still builds.

- [ ] **Step 5: Commit the workflow layer**

```bash
git add src/Librory.Infrastructure/Scanning src/Librory.Infrastructure/Wishlist src/Librory.Infrastructure/DependencyInjection.cs tests/Librory.Application.Tests
git commit -m "Implement in-memory scan workflow"
```

---

### Task 3: Expose scan and correction endpoints in the API

**Files:**
- Modify: `src/Librory.Api/Program.cs`
- Create: `src/Librory.Api/Endpoints/ScanEndpoints.cs`
- Create: `src/Librory.Api/Endpoints/LibraryEndpoints.cs`
- Create: `src/Librory.Api/Endpoints/WishlistEndpoints.cs`
- Create: `tests/Librory.Api.Tests/Librory.Api.Tests.csproj`
- Create: `tests/Librory.Api.Tests/ScanEndpointsTests.cs`

- [ ] **Step 1: Add the failing endpoint test**

Create `tests/Librory.Api.Tests/ScanEndpointsTests.cs`:

```csharp
using System.Net;

namespace Librory.Api.Tests;

public class ScanEndpointsTests
{
    [Fact]
    public async Task Post_scan_session_returns_ranked_candidates()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/api/scan-sessions", new
        {
            familyId = Guid.NewGuid(),
            preferredLanguage = "en",
            shelfPhotoPath = "shelf.jpg"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run the endpoint test and verify it fails**

Run:

```bash
dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj -v minimal --filter ScanEndpointsTests
```

Expected: fail until the API routes and `public partial class Program { }` are added.

- [ ] **Step 3: Wire the minimal API routes**

Add route modules and expose them from `Program.cs`:

```csharp
var scans = app.MapGroup("/api/scan-sessions");
scans.MapPost("/", StartShelfScanAsync);
scans.MapPatch("/{scanSessionId:guid}/candidates/{candidateId:guid}", ApplyCorrectionAsync);
```

Add `public partial class Program { }` at the end of `src/Librory.Api/Program.cs` so `WebApplicationFactory` can discover the host.

- [ ] **Step 4: Run the API tests and the solution build**

Run:

```bash
dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj -v minimal
dotnet build Librory.sln
```

Expected: the API test passes and the solution builds.

- [ ] **Step 5: Commit the API boundary**

```bash
git add src/Librory.Api tests/Librory.Api.Tests
git commit -m "Add scan API endpoints"
```

---

### Task 4: Build the first scan-review UI in React

**Files:**
- Modify: `src/Librory.Web/src/App.tsx`
- Create: `src/Librory.Web/src/lib/libroryApi.ts`
- Create: `src/Librory.Web/src/features/scan/ScanPage.tsx`
- Create: `src/Librory.Web/src/features/scan/ScanResultList.tsx`
- Create: `src/Librory.Web/src/features/scan/CorrectionPanel.tsx`
- Create: `src/Librory.Web/src/features/scan/types.ts`

- [ ] **Step 1: Add a failing UI build check**

Run:

```bash
npm run build
```

Expected: fail because the new route components and API client do not exist yet.

- [ ] **Step 2: Add the scan page and client**

Use a thin client that talks directly to the new API route:

```ts
export async function startShelfScan(request: StartShelfScanRequest): Promise<ScanSessionDto> {
  const response = await fetch('/api/scan-sessions', {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error('Failed to start shelf scan')
  }

  return response.json() as Promise<ScanSessionDto>
}
```

Render:
- a shelf upload or image path input
- a ranked candidate list
- a duplicate warning badge
- a correction drawer for rescans and manual titles
- bilingual labels driven by the selected language

- [ ] **Step 3: Wire the page into the app shell**

Keep the existing landing page structure, but add a route or toggle so the scan page can be reached without extra setup.

- [ ] **Step 4: Run the frontend build**

Run:

```bash
npm run build
```

Expected: the build passes and the scan page compiles.

- [ ] **Step 5: Commit the web slice**

```bash
git add src/Librory.Web
git commit -m "Add scan review web flow"
```

---

### Task 5: Add home intake, family library, and wishlist entry points

**Files:**
- Create: `src/Librory.Api/Endpoints/FamilyEndpoints.cs`
- Create: `src/Librory.Api/Endpoints/BookIntakeEndpoints.cs`
- Create: `src/Librory.Api/Endpoints/WishlistEndpoints.cs`
- Create: `src/Librory.Application/Intake/BookIntakeRequest.cs`
- Create: `src/Librory.Application/Intake/BookIntakeResultDto.cs`
- Create: `src/Librory.Web/src/features/library/FamilyLibraryPage.tsx`
- Create: `src/Librory.Web/src/features/intake/BookIntakePage.tsx`
- Create: `src/Librory.Web/src/features/wishlist/WishlistPage.tsx`
- Create: `src/Librory.Web/src/features/navigation/TopNav.tsx`
- Modify: `tests/Librory.Application.Tests/Librory.Application.Tests.csproj`

- [ ] **Step 1: Add the failing contract for intake and wishlist**

Create a minimal DTO test in `tests/Librory.Application.Tests/LibraryEntryTests.cs`:

```csharp
using Librory.Application.Intake;
using Librory.Application.Wishlist;

namespace Librory.Application.Tests;

public class LibraryEntryTests
{
    [Fact]
    public void BookIntake_request_keeps_optional_fields_optional()
    {
        var request = new BookIntakeRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Wonder",
            "cover.jpg",
            null,
            null,
            null,
            null,
            null);

        Assert.Equal("Wonder", request.Title);
        Assert.Equal("cover.jpg", request.CoverPhotoPath);
    }

    [Fact]
    public void Wishlist_item_has_work_and_edition_refs()
    {
        var item = new WishlistItemDto(Guid.NewGuid(), Guid.NewGuid(), "Wonder", "R.J. Palacio");

        Assert.Equal("Wonder", item.Title);
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

Run:

```bash
dotnet test tests/Librory.Application.Tests/Librory.Application.Tests.csproj -v minimal --filter LibraryEntryTests
```

Expected: fail until the new DTOs and endpoints are added.

- [ ] **Step 3: Add intake, family-library, and wishlist endpoints**

Keep the routes small and focused:

```csharp
var intake = app.MapGroup("/api/intake");
intake.MapPost("/", CreateBookIntakeAsync);

var family = app.MapGroup("/api/families");
family.MapGet("/{familyId:guid}", GetFamilySummaryAsync);

var wishlist = app.MapGroup("/api/wishlist");
wishlist.MapGet("/", GetWishlistAsync);
wishlist.MapPost("/", AddWishlistItemAsync);
```

The first version can return in-memory summaries so the Web app has a real shape to consume before persistence is added.

Use these request/result shapes:

```csharp
namespace Librory.Application.Intake;

public sealed record BookIntakeRequest(
    Guid FamilyId,
    Guid MemberId,
    string Title,
    string? CoverPhotoPath,
    Guid? BookEditionId,
    decimal? PurchasePrice,
    string? PurchaseStore,
    string? ShelfLocation,
    DateOnly? PurchaseDate);

public sealed record BookIntakeResultDto(
    Guid BookCopyId,
    string Title,
    string? CoverPhotoPath,
    string MemberDisplayName);
```

- [ ] **Step 4: Add the family and wishlist views**

Make the pages read from the same API client layer used by the scan page so they share language selection and error handling.

- [ ] **Step 5: Run the API and web builds**

Run:

```bash
dotnet build Librory.sln
npm run build
```

Expected: both build pipelines pass.

- [ ] **Step 6: Commit the family and wishlist slice**

```bash
git add src/Librory.Api src/Librory.Web tests/Librory.Application.Tests
git commit -m "Add family and wishlist entry points"
```

---

### Task 6: Prepare the persistence seam and finish the first release slice

**Files:**
- Modify: `src/Librory.Infrastructure/DependencyInjection.cs`
- Create: `src/Librory.Infrastructure/Persistence/LibroryDbContext.cs`
- Create: `src/Librory.Infrastructure/Persistence/EntityConfigurations/*.cs`
- Create: `src/Librory.Infrastructure/Persistence/Repositories/*.cs`
- Modify: `src/Librory.Domain/Models/Family.cs`
- Modify: `src/Librory.Domain/Models/BookCopy.cs`
- Modify: `src/Librory.AppHost/Program.cs`
- Modify: `src/Librory.Api/Program.cs`
- Create: `tests/Librory.Infrastructure.Tests/Librory.Infrastructure.Tests.csproj`

- [ ] **Step 1: Add a persistence-focused test**

Create `tests/Librory.Infrastructure.Tests/PersistenceModelTests.cs`:

```csharp
using Librory.Domain.Models;

namespace Librory.Infrastructure.Tests;

public class PersistenceModelTests
{
    [Fact]
    public void Family_exposes_book_copy_collection_for_persistence()
    {
        var family = new Family();

        Assert.NotNull(family.BookCopies);
    }
}
```

- [ ] **Step 2: Run the test and confirm the repository layer is not there yet**

Run:

```bash
dotnet test tests/Librory.Infrastructure.Tests/Librory.Infrastructure.Tests.csproj -v minimal
```

Expected: fail until the persistence project and DbContext exist.

- [ ] **Step 3: Add the persistence seam without changing the UI contract**

Introduce a `LibroryDbContext` and repository interfaces behind the existing application services so the Web app and API do not need to change when the store moves from memory to PostgreSQL.

Use these shapes as the starting point:

```csharp
using Librory.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Librory.Infrastructure.Persistence;

public sealed class LibroryDbContext : DbContext
{
    public DbSet<Family> Families => Set<Family>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<BookWork> BookWorks => Set<BookWork>();
    public DbSet<BookEdition> BookEditions => Set<BookEdition>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();

    public LibroryDbContext(DbContextOptions<LibroryDbContext> options) : base(options)
    {
    }
}

public interface IBookLibraryRepository
{
    Task<Family?> GetFamilyAsync(Guid familyId, CancellationToken cancellationToken);
    Task SaveBookCopyAsync(BookCopy copy, CancellationToken cancellationToken);
}
```

- [ ] **Step 4: Wire Aspire for local development**

Keep the AppHost responsible for starting the API and Web app first. Add the database resource only after the first slice works end to end.

- [ ] **Step 5: Run the final verification set**

Run:

```bash
dotnet build Librory.sln
npm run build
```

Expected: the solution and frontend both build cleanly.

- [ ] **Step 6: Commit the persistence seam**

```bash
git add src/Librory.Infrastructure src/Librory.AppHost src/Librory.Api tests
git commit -m "Prepare persistence seam for Librory"
```

---

## Self-Review

- Story-map P0 coverage: scan, ranking, duplicate warning, correction, family ownership, and bilingual UI are covered by Tasks 1-4.
- Story-map P1 coverage: intake, wishlist, and family library entry points are covered by Task 5.
- Data-model alignment: family roles, recommendation profile, and wishlist persistence are covered by Tasks 1 and 5.
- Architecture alignment: the API hosts orchestration, Infrastructure provides the first in-memory services, and the Web app stays as a thin client in Tasks 2-4.
- Placeholder scan: no TBD/TODO placeholders, no undefined helper names, and every file path is explicit.
