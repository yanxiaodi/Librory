# Book Recognition Job Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a signed-in user upload a shelf or book-cover photo, create an async recognition job, return OCR-derived book-title candidates with metadata enrichment, and let the web app poll the job until the user can review the results.

**Architecture:** Keep the first slice job-based and pollable. The API stores the uploaded image temporarily, creates a recognition job row, and lets a background processor turn OCR text into ranked title candidates, then enriches the strongest candidates through the existing book metadata search service. The web app keeps the current phone-friendly capture flow, but now polls the job until it can show metadata-enriched candidates for review.

**Tech Stack:** ASP.NET Core minimal APIs, EF Core, hosted services, Azure AI Vision OCR, Azure OpenAI vision fallback, React 19, React Router, Vitest, Testing Library, TypeScript.

## Global Constraints

- Async recognition job creation from an uploaded image
- Temporary image storage for the uploaded source photo
- OCR-first recognition with vision fallback for ambiguous cases
- Candidate ranking and noise reduction
- Metadata lookup for the top candidates via the existing book-metadata search API
- Job polling for status and final results
- The recognition result should stop at the candidate-and-metadata stage so the user can confirm or narrow down the list before any follow-on workflow runs
- The web UI should stay phone-friendly and camera-first

---

### Task 1: Add the recognition job API slice and persistence model

**Files:**
- Create: `src/Librory.Domain/Models/BookRecognitionJob.cs`
- Create: `src/Librory.Domain/Models/BookRecognitionJobStatus.cs`
- Create: `src/Librory.Application/Recognition/BookRecognitionCandidateDto.cs`
- Create: `src/Librory.Application/Recognition/BookRecognitionJobDto.cs`
- Create: `src/Librory.Application/Recognition/BookRecognitionJobResult.cs`
- Create: `src/Librory.Application/Recognition/IBookRecognitionJobService.cs`
- Create: `src/Librory.Api/Contracts/BookRecognitionCandidateResponse.cs`
- Create: `src/Librory.Api/Contracts/BookRecognitionJobResponse.cs`
- Create: `src/Librory.Api/Endpoints/BookRecognitionJobEndpoints.cs`
- Modify: `src/Librory.Infrastructure/Persistence/LibroryDbContext.cs`
- Create: `src/Librory.Infrastructure/Persistence/Configurations/BookRecognitionJobConfiguration.cs`
- Modify: `src/Librory.Infrastructure/DependencyInjection.cs`
- Modify: `src/Librory.Api/Program.cs`
- Create: `tests/Librory.Api.Tests/BookRecognitionJobEndpointsTests.cs`

**Interfaces:**
- Consumes: `IScanPhotoStorage.StoreTemporaryAsync(Stream content, string originalFileName, string contentType, CancellationToken cancellationToken)`
- Consumes: `IBookRecognitionJobService.CreateAsync(Guid familyId, string sourcePhotoPath, string? language, CancellationToken cancellationToken)`
- Consumes: `IBookRecognitionJobService.GetAsync(Guid familyId, Guid jobId, CancellationToken cancellationToken)`
- Produces: `POST /api/book-recognition-jobs` and `GET /api/book-recognition-jobs/{jobId}`

- [ ] **Step 1: Write the failing API tests**

```csharp
[Fact]
public async Task Posting_a_photo_creates_a_queued_book_recognition_job()
{
    await using var factory = await ApiFactory.CreateAsync();
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        HandleCookies = true,
    });

    await client.PostAsync("/dev/bootstrap", content: null);

    var content = new MultipartFormDataContent();
    var image = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });
    image.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
    content.Add(image, "photo", "shelf.jpg");

    var response = await client.PostAsync("/api/book-recognition-jobs", content);

    Assert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);
    var created = await response.Content.ReadFromJsonAsync<BookRecognitionJobResponse>();
    Assert.NotNull(created);
    Assert.NotEqual(Guid.Empty, created!.JobId);
    Assert.Equal(BookRecognitionJobStatus.Queued, created.Status);
    Assert.Empty(created.Candidates);
}
```

```csharp
[Fact]
public async Task Getting_a_book_recognition_job_returns_the_current_state()
{
    await using var factory = await ApiFactory.CreateAsync();
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        HandleCookies = true,
    });

    await client.PostAsync("/dev/bootstrap", content: null);

    var response = await client.GetAsync("/api/book-recognition-jobs/00000000-0000-0000-0000-000000000001");

    Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
}
```

- [ ] **Step 2: Run the tests and confirm they fail for the new slice**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --filter FullyQualifiedName~BookRecognitionJobEndpointsTests -v minimal`
Expected: fail because the endpoint, DTOs, and persistence model do not exist yet.

- [ ] **Step 3: Implement the job model, service contract, and endpoints**

```csharp
public enum BookRecognitionJobStatus
{
    Queued = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
}
```

```csharp
public interface IBookRecognitionJobService
{
    Task<BookRecognitionJobDto> CreateAsync(
        Guid familyId,
        string sourcePhotoPath,
        string? language,
        CancellationToken cancellationToken);

    Task<BookRecognitionJobDto?> GetAsync(
        Guid familyId,
        Guid jobId,
        CancellationToken cancellationToken);
}
```

```csharp
group.MapPost(string.Empty, CreateBookRecognitionJobAsync)
    .WithName("CreateBookRecognitionJob")
    .WithSummary("Create a book recognition job.")
    .WithDescription("Stores an uploaded photo temporarily and creates an async job that will extract book-title candidates.")
    .Produces<BookRecognitionJobResponse>(StatusCodes.Status202Accepted)
    .ProducesValidationProblem()
    .Produces(StatusCodes.Status401Unauthorized);
```

The endpoint should:

1. Require the current family context.
2. Validate one uploaded image file named `photo`.
3. Reuse `IScanPhotoStorage` for temporary storage.
4. Create the recognition job immediately with status `Queued`.
5. Return `202 Accepted` with the job id and initial status.
6. Delete the uploaded file if job creation fails after storage.

Persist the job in a single EF Core table with the current family id, source photo path, job status, timestamps, failure details, and a serialized result payload so polling can round-trip the full candidate list without re-running OCR.

- [ ] **Step 4: Run the targeted tests and make sure they pass**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --filter FullyQualifiedName~BookRecognitionJobEndpointsTests -v minimal`
Expected: pass.

- [ ] **Step 5: Commit the API slice**

```bash
git add src/Librory.Domain/Models/BookRecognitionJob.cs src/Librory.Domain/Models/BookRecognitionJobStatus.cs src/Librory.Application/Recognition/BookRecognitionCandidateDto.cs src/Librory.Application/Recognition/BookRecognitionJobDto.cs src/Librory.Application/Recognition/BookRecognitionJobResult.cs src/Librory.Application/Recognition/IBookRecognitionJobService.cs src/Librory.Api/Contracts/BookRecognitionCandidateResponse.cs src/Librory.Api/Contracts/BookRecognitionJobResponse.cs src/Librory.Api/Endpoints/BookRecognitionJobEndpoints.cs src/Librory.Infrastructure/Persistence/LibroryDbContext.cs src/Librory.Infrastructure/Persistence/Configurations/BookRecognitionJobConfiguration.cs src/Librory.Infrastructure/DependencyInjection.cs src/Librory.Api/Program.cs tests/Librory.Api.Tests/BookRecognitionJobEndpointsTests.cs
git commit -m "feat: add book recognition job api"
```

---

### Task 2: Add the OCR, ranking, and enrichment pipeline

**Files:**
- Create: `src/Librory.Application/Recognition/IBookRecognitionPipeline.cs`
- Create: `src/Librory.Application/Recognition/IOcrTextExtractionService.cs`
- Create: `src/Librory.Application/Recognition/RecognizedTextBlock.cs`
- Create: `src/Librory.Infrastructure/Recognition/BookRecognitionPipeline.cs`
- Create: `src/Librory.Infrastructure/Recognition/BookTitleCandidateRanker.cs`
- Create: `src/Librory.Infrastructure/Recognition/AzureAiVisionTextExtractionService.cs`
- Create: `src/Librory.Infrastructure/Recognition/AzureOpenAiVisionFallbackService.cs`
- Create: `src/Librory.Infrastructure/Recognition/BookRecognitionJobProcessor.cs`
- Create: `src/Librory.Infrastructure/Recognition/BookRecognitionJobProcessorHostedService.cs`
- Modify: `src/Librory.Infrastructure/DependencyInjection.cs`
- Create: `tests/Librory.Application.Tests/BookTitleCandidateRankerTests.cs`
- Create: `tests/Librory.Application.Tests/BookRecognitionPipelineTests.cs`

**Interfaces:**
- Consumes: `IBookMetadataSearchService.SearchByTitleAsync(string title, string? language, int maxResults, CancellationToken cancellationToken)`
- Consumes: `IOcrTextExtractionService.ExtractAsync(string sourcePhotoPath, CancellationToken cancellationToken)`
- Produces: `IBookRecognitionPipeline.RecognizeAsync(string sourcePhotoPath, string? language, CancellationToken cancellationToken)`
- Produces: `IBookRecognitionJobProcessor.ProcessQueuedJobsAsync(CancellationToken cancellationToken)`

- [ ] **Step 1: Write the failing ranking and pipeline tests**

```csharp
[Fact]
public void Ranker_prefers_title_like_spans_and_downranks_noise()
{
    var ranker = new BookTitleCandidateRanker();

    var result = ranker.Rank(new[]
    {
        new RecognizedTextBlock("The Left Hand of Darkness", confidence: 0.98m, left: 100, top: 120, right: 520, bottom: 180, isVertical: false),
        new RecognizedTextBlock("Ursula K. Le Guin", confidence: 0.95m, left: 110, top: 190, right: 430, bottom: 230, isVertical: false),
        new RecognizedTextBlock("A masterpiece of science fiction", confidence: 0.82m, left: 80, top: 240, right: 560, bottom: 290, isVertical: false),
    });

    Assert.Equal("The Left Hand of Darkness", result.First().DisplayTitle);
    Assert.DoesNotContain(result, candidate => candidate.DisplayTitle.Contains("masterpiece", StringComparison.OrdinalIgnoreCase));
}
```

```csharp
[Fact]
public async Task Pipeline_uses_ocr_and_enriches_top_candidates_with_metadata()
{
    var ocr = new FakeOcrTextExtractionService(new[]
    {
        new RecognizedTextBlock("Dune", 0.99m, 100, 100, 240, 180, false),
        new RecognizedTextBlock("Frank Herbert", 0.97m, 100, 200, 260, 240, false),
    });

    var metadata = new FakeBookMetadataSearchService("Dune", new[]
    {
        new BookMetadataCandidate("google-books", "source-1", "Dune", null, new[] { "Frank Herbert" }, "Ace", "1965", "en", null, "9780441013593", "9780441013593", null, null),
    });

    var pipeline = new BookRecognitionPipeline(ocr, metadata, new BookTitleCandidateRanker());
    var result = await pipeline.RecognizeAsync("/tmp/shelf.jpg", "en", CancellationToken.None);

    Assert.Single(result.Candidates);
    Assert.Single(result.Candidates[0].MetadataMatches);
    Assert.Equal("Dune", result.Candidates[0].DisplayTitle);
}
```

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `dotnet test tests/Librory.Application.Tests/Librory.Application.Tests.csproj --filter "FullyQualifiedName~BookTitleCandidateRankerTests|FullyQualifiedName~BookRecognitionPipelineTests" -v minimal`
Expected: fail because the ranker, pipeline, and OCR abstraction do not exist yet.

- [ ] **Step 3: Implement the pipeline and background processor**

```csharp
public interface IBookRecognitionPipeline
{
    Task<BookRecognitionJobResult> RecognizeAsync(
        string sourcePhotoPath,
        string? language,
        CancellationToken cancellationToken);
}
```

```csharp
public interface IOcrTextExtractionService
{
    Task<IReadOnlyList<RecognizedTextBlock>> ExtractAsync(
        string sourcePhotoPath,
        CancellationToken cancellationToken);
}
```

The pipeline should:

1. Call Azure AI Vision OCR first.
2. Generate multiple title candidates from grouped text blocks.
3. Rank candidates with heuristics that favor title-like and spine-like text while demoting author lines, blurbs, publisher text, and ISBN noise.
4. Use Azure OpenAI vision only when OCR confidence is low or the layout is unusually noisy.
5. Enrich the strongest candidates with `IBookMetadataSearchService`.
6. Return the completed result so the job processor can persist it.

The job processor should:

1. Claim queued jobs from the database.
2. Mark a job as running before processing starts.
3. Save the completed candidate payload on success.
4. Mark the job failed with a recoverable message if OCR or fallback processing fails.
5. Leave partial OCR candidates in place when metadata lookup fails for some candidates.

Register the processor behind a hosted service so the API process keeps polling for queued jobs without blocking requests.

- [ ] **Step 4: Run the targeted tests and make sure they pass**

Run: `dotnet test tests/Librory.Application.Tests/Librory.Application.Tests.csproj --filter "FullyQualifiedName~BookTitleCandidateRankerTests|FullyQualifiedName~BookRecognitionPipelineTests" -v minimal`
Expected: pass.

- [ ] **Step 5: Commit the pipeline slice**

```bash
git add src/Librory.Application/Recognition/IBookRecognitionPipeline.cs src/Librory.Application/Recognition/IOcrTextExtractionService.cs src/Librory.Application/Recognition/RecognizedTextBlock.cs src/Librory.Infrastructure/Recognition/BookRecognitionPipeline.cs src/Librory.Infrastructure/Recognition/BookTitleCandidateRanker.cs src/Librory.Infrastructure/Recognition/AzureAiVisionTextExtractionService.cs src/Librory.Infrastructure/Recognition/AzureOpenAiVisionFallbackService.cs src/Librory.Infrastructure/Recognition/BookRecognitionJobProcessor.cs src/Librory.Infrastructure/Recognition/BookRecognitionJobProcessorHostedService.cs src/Librory.Infrastructure/DependencyInjection.cs tests/Librory.Application.Tests/BookTitleCandidateRankerTests.cs tests/Librory.Application.Tests/BookRecognitionPipelineTests.cs
git commit -m "feat: add book recognition pipeline"
```

---

### Task 3: Wire the web app to create, poll, and render recognition jobs

**Files:**
- Create: `src/Librory.Web/src/lib/bookRecognitionApi.ts`
- Modify: `src/Librory.Web/src/pages/ScansPage.tsx`
- Modify: `src/Librory.Web/src/pages/ScansPage.test.tsx`
- Create: `src/Librory.Web/src/components/scans/BookRecognitionResults.tsx`

**Interfaces:**
- Consumes: `createBookRecognitionJob(file: File): Promise<BookRecognitionJobResponse>`
- Consumes: `getBookRecognitionJob(jobId: string): Promise<BookRecognitionJobResponse>`
- Produces: a page state machine with `idle`, `uploading`, `polling`, `ready`, and `error`

- [ ] **Step 1: Write the failing page tests**

```tsx
it('uploads a photo and renders the recognition candidates after polling', async () => {
  const user = userEvent.setup();

  vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
    const url = String(input);

    if (url.endsWith('/api/book-recognition-jobs') && !url.includes('/api/book-recognition-jobs/')) {
      return new Response(JSON.stringify({
        jobId: 'job-1',
        familyId: 'family-1',
        status: 0,
        sourcePhotoPath: '/tmp/Librory/scan-uploads/shelf.jpg',
        candidates: [],
        warnings: [],
      }), { status: 202, headers: { 'Content-Type': 'application/json' } });
    }

    return new Response(JSON.stringify({
      jobId: 'job-1',
      familyId: 'family-1',
      status: 2,
      sourcePhotoPath: '/tmp/Librory/scan-uploads/shelf.jpg',
      candidates: [
        {
          candidateId: 'candidate-1',
          displayTitle: 'Dune',
          evidenceText: 'DUNE',
          rank: 1,
          metadataMatches: [],
        },
      ],
      warnings: [],
    }), { status: 200, headers: { 'Content-Type': 'application/json' } });
  }));

  render(<ScansPage />);

  await user.upload(screen.getByLabelText(/shelf photo/i), new File(['fake'], 'shelf.jpg', { type: 'image/jpeg' }));

  expect(await screen.findByText(/polling/i)).toBeVisible();
  expect(await screen.findByText(/dune/i)).toBeVisible();
});
```

- [ ] **Step 2: Run the page test and confirm it fails**

Run from `src/Librory.Web`: `npm run test:run -- src/pages/ScansPage.test.tsx`
Expected: fail because the page only knows about the upload-to-scan-session flow today.

- [ ] **Step 3: Implement the API helper and page state machine**

```ts
export async function createBookRecognitionJob(file: File): Promise<BookRecognitionJobResponse> {
  const formData = new FormData();
  formData.append('photo', file);

  const response = await fetch('/api/book-recognition-jobs', {
    method: 'POST',
    credentials: 'include',
    body: formData,
  });

  if (!response.ok) {
    throw new Error(`Book recognition job creation failed (${response.status}).`);
  }

  return response.json() as Promise<BookRecognitionJobResponse>;
}
```

The page should:

1. Keep the camera-first image picker.
2. Call the new recognition job endpoint instead of the scan-session upload endpoint.
3. Poll the job until it reaches `Succeeded` or `Failed`.
4. Show the current status while polling.
5. Render the candidate list and matched metadata when the job completes.
6. Show a retry state if the job fails.

Keep the current mobile-friendly layout, but replace the old scan-session language with recognition-job language so the user can see that the app is identifying book titles before any later workflow runs.

- [ ] **Step 4: Run the frontend test and make sure it passes**

Run from `src/Librory.Web`: `npm run test:run -- src/pages/ScansPage.test.tsx`
Expected: pass.

- [ ] **Step 5: Commit the web slice**

```bash
git add src/Librory.Web/src/lib/bookRecognitionApi.ts src/Librory.Web/src/pages/ScansPage.tsx src/Librory.Web/src/pages/ScansPage.test.tsx src/Librory.Web/src/components/scans/BookRecognitionResults.tsx
git commit -m "feat: add book recognition polling ui"
```

---

### Task 4: Update docs and run the full verification pass

**Files:**
- Modify: `docs/api-reference.md`
- Modify: `docs/frontend-integration-guide.md`
- Modify: `docs/deployment.md`
- Modify: `docs/backend-story-map.md`
- Modify: `docs/story-map-mvp.md`
- Create: `docs/devlog/2026-08-03-book-recognition-job.md`

**Interfaces:**
- Consumes: the completed recognition API, pipeline, and web flow
- Produces: docs that explain the new job endpoints and deployment requirements

- [ ] **Step 1: Update the docs to describe the new flow**

Document:

1. `POST /api/book-recognition-jobs`
2. `GET /api/book-recognition-jobs/{jobId}`
3. The queued/running/succeeded/failed lifecycle
4. The fact that recognition stops after candidate enrichment and does not jump directly into recommendation
5. The Azure Vision OCR and Azure OpenAI fallback configuration required in deployment

Add a short note in the frontend integration guide showing that the web app now polls the recognition job before it renders results.

- [ ] **Step 2: Run the full API and web test suites**

Run:

```bash
dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj -v minimal
dotnet test tests/Librory.Application.Tests/Librory.Application.Tests.csproj -v minimal
dotnet test tests/Librory.Domain.Tests/Librory.Domain.Tests.csproj -v minimal
```

Run from `src/Librory.Web`:

```bash
npm run test:run
```

Expected: all tests pass, including the new recognition job coverage.

- [ ] **Step 3: Inspect the diff for scope**

Run: `git status --short`
Expected: only recognition-job files, related tests, and the docs above should be modified.

- [ ] **Step 4: Commit the docs and final verification pass**

```bash
git add docs/api-reference.md docs/frontend-integration-guide.md docs/deployment.md docs/backend-story-map.md docs/story-map-mvp.md docs/devlog/2026-08-03-book-recognition-job.md
git commit -m "docs: add book recognition job flow"
```

## Coverage Check

This plan covers:

- a pollable recognition job API
- temporary upload storage reuse
- OCR-first candidate generation with vision fallback
- candidate ranking and metadata enrichment
- background processing for async completion
- phone-friendly web polling and result display
- deployment and integration docs for the new flow

It intentionally stops before recommendation generation and intake promotion because the story boundary keeps those as later explicit steps.
