# Story 12 External Metadata Import Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a signed-in user search external book metadata by title and confirm one normalized result into Librory's canonical catalog as a `BookWork` with an optional first `BookEdition`.

**Architecture:** Keep the existing metadata search endpoint read-only and provider-neutral. Add a small canonical import service behind a new authenticated API endpoint, and reuse the existing `BookWork` / `BookEdition` model instead of introducing a separate import session or a parallel catalog shape. Use exact ISBN reuse as the only dedupe rule for import so the catalog stays simple and predictable.

**Tech Stack:** ASP.NET Core minimal APIs, EF Core, PostgreSQL, xUnit, WebApplicationFactory

## Global Constraints

- External metadata search by title through the existing provider abstraction
- A confirm-and-import API that creates canonical catalog records from a normalized metadata candidate
- Canonical import that reuses the current `BookWork` and `BookEdition` model
- Provenance preservation for imported metadata fields
- API and integration tests for search and import
- The import flow should stop at canonical catalog creation and not introduce import sessions or periodic synchronization
- The import endpoint should not require family context
- Exact ISBN matches should reuse an existing canonical edition instead of creating a duplicate

---

### Task 1: Add import contracts and failing API coverage

**Files:**
- Create: `src/Librory.Api/Contracts/BookMetadataImportCandidateRequest.cs`
- Create: `src/Librory.Api/Contracts/BookMetadataImportRequest.cs`
- Create: `tests/Librory.Api.Tests/BookMetadataImportEndpointsTests.cs`

**Interfaces:**
- Consumes: `POST /api/book-metadata/import`
- Consumes: `BookWorkResponse` from the existing book-work API
- Produces: a normalized import request shape that wraps one candidate

- [ ] **Step 1: Write the failing API tests**

```csharp
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
```

```csharp
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
```

```csharp
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
```

- [ ] **Step 2: Run the tests and confirm they fail for the new slice**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --filter FullyQualifiedName~BookMetadataImportEndpointsTests -v minimal`
Expected: fail because the import contracts, endpoint, and service do not exist yet.

- [ ] **Step 3: Commit the test-first slice**

```bash
git add src/Librory.Api/Contracts/BookMetadataImportCandidateRequest.cs src/Librory.Api/Contracts/BookMetadataImportRequest.cs tests/Librory.Api.Tests/BookMetadataImportEndpointsTests.cs
git commit -m "test: cover book metadata import"
```

---

### Task 2: Add the canonical import service and endpoint

**Files:**
- Create: `src/Librory.Application/Metadata/IBookMetadataImportService.cs`
- Create: `src/Librory.Application/Metadata/BookMetadataImportResult.cs`
- Create: `src/Librory.Infrastructure/Metadata/BookMetadataImportService.cs`
- Modify: `src/Librory.Infrastructure/DependencyInjection.cs`
- Modify: `src/Librory.Api/Endpoints/BookMetadataEndpoints.cs`

**Interfaces:**
- Consumes: `IBookMetadataImportService.ImportAsync(BookMetadataCandidate candidate, CancellationToken cancellationToken)`
- Consumes: `BookMetadataCandidate` from the application metadata layer
- Produces: `BookMetadataImportResult(BookWork Work, bool CreatedNew)`
- Produces: `POST /api/book-metadata/import`

- [ ] **Step 1: Implement the import contract and service**

```csharp
public interface IBookMetadataImportService
{
    Task<BookMetadataImportResult> ImportAsync(
        BookMetadataCandidate candidate,
        CancellationToken cancellationToken);
}

public sealed record BookMetadataImportResult(
    BookWork Work,
    bool CreatedNew);
```

The service should:

1. Validate that `candidate.Title` is present.
2. Normalize authors into a single canonical author string in provider order.
3. Map `candidate.Description` into `BookWork.Summary`.
4. Map `candidate.Subtitle` into `BookEdition.Subtitle`.
5. Prefer `candidate.Isbn13` over `candidate.Isbn10`.
6. Parse a four-digit publication year when possible.
7. Preserve `Source`, `SourceId`, and capture time in metadata provenance objects.
8. Reuse an existing edition when the exact ISBN already exists.
9. Create a new work and optional first edition when no exact ISBN match exists.

The import code should use a serializable transaction around the ISBN lookup and insert path so two concurrent imports cannot create duplicate editions for the same ISBN.

- [ ] **Step 2: Make the endpoint call the import service**

The endpoint should:

1. Require authentication.
2. Accept a single normalized candidate inside the request body.
3. Validate `candidate.source`, `candidate.sourceId`, and `candidate.title`.
4. Convert the request DTO into `BookMetadataCandidate`.
5. Call `IBookMetadataImportService`.
6. Return `201 Created` when the service created a new canonical record.
7. Return `200 OK` when the service reused an existing canonical record.
8. Return the existing `BookWorkResponse` payload in both cases.

The existing `GET /api/book-metadata/search` endpoint should stay read-only and unchanged except for any shared contract reuse needed by the new import flow.

- [ ] **Step 3: Run the targeted tests and make sure they pass**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --filter FullyQualifiedName~BookMetadataImportEndpointsTests -v minimal`
Expected: pass.

- [ ] **Step 4: Commit the import slice**

```bash
git add src/Librory.Application/Metadata/IBookMetadataImportService.cs src/Librory.Application/Metadata/BookMetadataImportResult.cs src/Librory.Infrastructure/Metadata/BookMetadataImportService.cs src/Librory.Infrastructure/DependencyInjection.cs src/Librory.Api/Endpoints/BookMetadataEndpoints.cs
git commit -m "feat: add book metadata import"
```

---

### Task 3: Update docs and run the full verification pass

**Files:**
- Modify: `docs/api-reference.md`
- Modify: `docs/backend-story-map.md`
- Create: `docs/devlog/2026-08-03-story-12-external-metadata-import.md`

**Interfaces:**
- Consumes: the completed metadata search and import API
- Produces: docs that explain the new import flow and story status

- [ ] **Step 1: Update the docs to describe the import flow**

Document:

1. `GET /api/book-metadata/search`
2. `POST /api/book-metadata/import`
3. The normalized candidate shape the frontend should post back
4. The 200-versus-201 behavior based on exact ISBN reuse
5. The fact that canonical import stops at `BookWork` plus optional first `BookEdition`

Add a short note in the backend story map marking `story-12` as delivered once the code is in.

- [ ] **Step 2: Run the full API test suite**

Run:

```bash
dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj -v minimal
dotnet test tests/Librory.Application.Tests/Librory.Application.Tests.csproj -v minimal
dotnet test tests/Librory.Domain.Tests/Librory.Domain.Tests.csproj -v minimal
```

Expected: all tests pass, including the new metadata import coverage.

- [ ] **Step 3: Inspect the diff for scope**

Run: `git status --short`
Expected: only metadata-import code, related tests, and the docs above should be modified.

- [ ] **Step 4: Commit the docs and final verification pass**

```bash
git add docs/api-reference.md docs/backend-story-map.md docs/devlog/2026-08-03-story-12-external-metadata-import.md
git commit -m "docs: add book metadata import flow"
```

## Coverage Check

This plan covers:

- a read-only provider-neutral metadata search endpoint
- a canonical import endpoint that accepts one normalized candidate
- reuse of existing `BookWork` and `BookEdition` catalog modeling
- ISBN-based duplicate avoidance during import
- provenance-preserving import of external metadata
- API and integration tests for successful import, reuse, and validation failure
- documentation updates that mark `story-12` delivered

It intentionally stops before import sessions, periodic sync, provider routing, recommendation scoring, or shelf recognition because the story boundary keeps those as later work.
