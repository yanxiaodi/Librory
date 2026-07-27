# Shelf Photo Upload Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a signed-in user on the web app upload a shelf photo from a phone browser, store it temporarily, create a scan session, and show the app in a scanning state while expired temporary data is cleaned up daily.

**Architecture:** The first slice is a direct multipart upload from the web client to the API. The API stores the image in a temporary local store, creates a scan session with a one-day retention window, and returns the existing scan-session response shape. A separate cleanup service deletes expired temporary files and sessions once per day.

**Tech Stack:** ASP.NET Core minimal APIs, EF Core, xUnit, React 19, React Router, Vitest, Testing Library, TypeScript.

## Global Constraints

- Web-only photo capture and upload
- Temporary image storage
- Scan session creation
- Immediate "scanning" UI state after upload
- Short-lived retention for incomplete or failed uploads
- Daily cleanup of expired temporary scan data
- API and frontend tests for the upload path

---

### Task 1: Add the shelf photo upload endpoint and temporary storage

**Files:**
- Create: `src/Librory.Application/Scanning/IScanPhotoStorage.cs`
- Create: `src/Librory.Infrastructure/Scanning/LocalScanPhotoStorage.cs`
- Modify: `src/Librory.Api/Endpoints/ScanSessionEndpoints.cs`
- Modify: `src/Librory.Infrastructure/DependencyInjection.cs`
- Create: `tests/Librory.Api.Tests/ScanUploadEndpointsTests.cs`

**Interfaces:**
- Consumes: `IScanSessionService.StartShelfScanAsync(ScanShelfRequest request, CancellationToken cancellationToken)`
- Produces: `Task<string> StoreTemporaryAsync(Stream content, string originalFileName, string contentType, CancellationToken cancellationToken)` and `Task DeleteAsync(string shelfPhotoPath, CancellationToken cancellationToken)` on `IScanPhotoStorage`
- Produces: `POST /api/family/current/scan-sessions/uploads` that accepts `multipart/form-data` with a single image field named `photo`

- [ ] **Step 1: Write the failing API integration tests**

```csharp
[Fact]
public async Task Shelf_photo_upload_creates_a_scan_session()
{
    await using var factory = await ApiFactory.CreateAsync();
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        HandleCookies = true,
    });

    var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
    bootstrapResponse.EnsureSuccessStatusCode();

    var content = new MultipartFormDataContent();
    var image = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });
    image.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
    content.Add(image, "photo", "shelf.jpg");

    var response = await client.PostAsync("/api/family/current/scan-sessions/uploads", content);

    Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    var created = await response.Content.ReadFromJsonAsync<ScanSessionResponse>();
    Assert.NotNull(created);
    Assert.EndsWith(".jpg", created!.ShelfPhotoPath, StringComparison.OrdinalIgnoreCase);
}
```

```csharp
[Fact]
public async Task Shelf_photo_upload_rejects_missing_or_non_image_files()
{
    await using var factory = await ApiFactory.CreateAsync();
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        HandleCookies = true,
    });

    var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
    bootstrapResponse.EnsureSuccessStatusCode();

    var missingFileResponse = await client.PostAsync(
        "/api/family/current/scan-sessions/uploads",
        new MultipartFormDataContent());

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, missingFileResponse.StatusCode);

    var invalidContent = new MultipartFormDataContent();
    var textFile = new StringContent("not an image");
    textFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
    invalidContent.Add(textFile, "photo", "shelf.txt");

    var invalidFileResponse = await client.PostAsync(
        "/api/family/current/scan-sessions/uploads",
        invalidContent);

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalidFileResponse.StatusCode);
}
```

- [ ] **Step 2: Run the tests and confirm they fail for the new endpoint**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --filter FullyQualifiedName~ScanUploadEndpointsTests -v minimal`
Expected: fail because `POST /api/family/current/scan-sessions/uploads` and `IScanPhotoStorage` do not exist yet.

- [ ] **Step 3: Implement the storage and endpoint path**

```csharp
public interface IScanPhotoStorage
{
    Task<string> StoreTemporaryAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken);

    Task DeleteAsync(string shelfPhotoPath, CancellationToken cancellationToken);
}
```

```csharp
group.MapPost("uploads", UploadShelfPhotoAsync)
    .WithName("UploadShelfPhoto")
    .WithSummary("Upload a shelf photo.")
    .Produces<ScanSessionResponse>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status401Unauthorized);

static async Task<IResult> UploadShelfPhotoAsync(
    IFormFile photo,
    ICurrentFamilyContextAccessor accessor,
    IScanPhotoStorage photoStorage,
    IScanSessionService scanSessionService,
    CancellationToken cancellationToken)
{
    // Validate auth, file presence, file type, and file size before writing anything.
    var current = accessor.Current;
    if (current is null)
    {
        return Results.Unauthorized();
    }

    if (photo is null || photo.Length == 0)
    {
        return Results.BadRequest(new { error = "Photo is required." });
    }

    var storedPath = await photoStorage.StoreTemporaryAsync(
        photo.OpenReadStream(),
        photo.FileName,
        photo.ContentType,
        cancellationToken);

    try
    {
        var dto = await scanSessionService.StartShelfScanAsync(
            new ScanShelfRequest(
                current.FamilyId,
                current.PreferredLanguage == PreferredLanguage.Chinese ? "zh" : "en",
                storedPath,
                TimeSpan.FromDays(1)),
            cancellationToken);

        return Results.Created(
            $"/api/family/current/scan-sessions/{dto.ScanSessionId}",
            ToResponse(dto));
    }
    catch
    {
        await photoStorage.DeleteAsync(storedPath, cancellationToken);
        throw;
    }
}
```

Implement the endpoint so it:

1. Requires the current family context.
2. Validates a single uploaded image.
3. Stores the file through `IScanPhotoStorage`.
4. Calls `StartShelfScanAsync` with the stored path and a one-day retention window.
5. Deletes the temp file if scan-session creation fails after upload.

Use `Path.GetTempPath()` plus a `Librory/scan-uploads` subfolder for the local storage root so the feature does not need new configuration files in this slice.

- [ ] **Step 4: Run the targeted API tests and make sure they pass**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --filter FullyQualifiedName~ScanUploadEndpointsTests -v minimal`
Expected: pass.

- [ ] **Step 5: Commit the backend upload slice**

```bash
git add src/Librory.Application/Scanning/IScanPhotoStorage.cs src/Librory.Infrastructure/Scanning/LocalScanPhotoStorage.cs src/Librory.Api/Endpoints/ScanSessionEndpoints.cs src/Librory.Infrastructure/DependencyInjection.cs tests/Librory.Api.Tests/ScanUploadEndpointsTests.cs
git commit -m "feat: add shelf photo upload endpoint"
```

---

### Task 2: Add temporary scan cleanup and daily scheduling

**Files:**
- Create: `src/Librory.Application/Scanning/IScanSessionCleanupService.cs`
- Create: `src/Librory.Infrastructure/Scanning/ExpiredScanSessionCleanupService.cs`
- Create: `src/Librory.Infrastructure/Scanning/ScanCleanupHostedService.cs`
- Modify: `src/Librory.Infrastructure/DependencyInjection.cs`
- Create: `tests/Librory.Api.Tests/ScanCleanupTests.cs`

**Interfaces:**
- Consumes: `IScanPhotoStorage.DeleteAsync(string shelfPhotoPath, CancellationToken cancellationToken)`
- Consumes: `LibroryDbContext.ScanSessions` with `ExpiresAt` and `ShelfPhotoPath`
- Produces: `Task<int> DeleteExpiredTemporaryScanDataAsync(DateTimeOffset asOf, CancellationToken cancellationToken)` on `IScanSessionCleanupService`

- [ ] **Step 1: Write the failing cleanup test**

```csharp
[Fact]
public async Task Cleanup_deletes_expired_scan_sessions_and_their_temp_files()
{
    await using var factory = await ApiFactory.CreateAsync();
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        HandleCookies = true,
    });

    var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
    bootstrapResponse.EnsureSuccessStatusCode();

    using var scope = factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();
    var cleanup = scope.ServiceProvider.GetRequiredService<IScanSessionCleanupService>();

    var family = await db.Families.SingleAsync();
    var tempFilePath = Path.Combine(Path.GetTempPath(), "Librory", "scan-uploads", $"{Guid.NewGuid():N}.jpg");
    Directory.CreateDirectory(Path.GetDirectoryName(tempFilePath)!);
    await File.WriteAllBytesAsync(tempFilePath, new byte[] { 0x01, 0x02, 0x03 }, CancellationToken.None);

    var session = ScanSession.Create(family, tempFilePath, TimeSpan.FromMinutes(1));
    db.ScanSessions.Add(session);
    await db.SaveChangesAsync();

    db.Entry(session).Property(x => x.ExpiresAt).CurrentValue = DateTimeOffset.UtcNow.AddDays(-1);
    await db.SaveChangesAsync();

    var deleted = await cleanup.DeleteExpiredTemporaryScanDataAsync(DateTimeOffset.UtcNow, CancellationToken.None);

    Assert.Equal(1, deleted);
    Assert.Empty(await db.ScanSessions.ToListAsync());
    Assert.False(File.Exists(tempFilePath));
}
```

- [ ] **Step 2: Run the test and confirm it fails**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --filter FullyQualifiedName~ScanCleanupTests -v minimal`
Expected: fail because the cleanup service and hosted service are not implemented yet.

- [ ] **Step 3: Implement the cleanup service and hosted service**

```csharp
public interface IScanSessionCleanupService
{
    Task<int> DeleteExpiredTemporaryScanDataAsync(DateTimeOffset asOf, CancellationToken cancellationToken);
}
```

```csharp
public sealed class ScanCleanupHostedService : BackgroundService
{
    private readonly IScanSessionCleanupService cleanupService;

    public ScanCleanupHostedService(IScanSessionCleanupService cleanupService)
    {
        this.cleanupService = cleanupService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));

        await cleanupService.DeleteExpiredTemporaryScanDataAsync(DateTimeOffset.UtcNow, stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await cleanupService.DeleteExpiredTemporaryScanDataAsync(DateTimeOffset.UtcNow, stoppingToken);
        }
    }
}
```

The cleanup implementation should:

1. Load expired scan sessions.
2. Delete each stored temp image via `IScanPhotoStorage`.
3. Remove the expired sessions from the database.
4. Save changes once per sweep.

Register the hosted service in `src/Librory.Infrastructure/DependencyInjection.cs` so the cleanup runs automatically in the API process.

- [ ] **Step 4: Run the cleanup tests and make sure they pass**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --filter FullyQualifiedName~ScanCleanupTests -v minimal`
Expected: pass.

- [ ] **Step 5: Commit the cleanup slice**

```bash
git add src/Librory.Application/Scanning/IScanSessionCleanupService.cs src/Librory.Infrastructure/Scanning/ExpiredScanSessionCleanupService.cs src/Librory.Infrastructure/Scanning/ScanCleanupHostedService.cs src/Librory.Infrastructure/DependencyInjection.cs tests/Librory.Api.Tests/ScanCleanupTests.cs
git commit -m "feat: add scan cleanup service"
```

---

### Task 3: Wire the Scans page to capture and upload a shelf photo

**Files:**
- Modify: `src/Librory.Web/src/pages/ScansPage.tsx`
- Create: `src/Librory.Web/src/lib/scansApi.ts`
- Create: `src/Librory.Web/src/pages/ScansPage.test.tsx`

**Interfaces:**
- Consumes: `uploadShelfPhoto(file: File): Promise<ScanSessionResponse>` from `src/Librory.Web/src/lib/scansApi.ts`
- Produces: a page state machine with `idle`, `uploading`, `scanning`, and `error` states

- [ ] **Step 1: Write the failing page test**

```tsx
it('uploads a shelf photo and switches to scanning state', async () => {
  const user = userEvent.setup()

  vi.stubGlobal('fetch', vi.fn(async () => {
    return new Response(
      JSON.stringify({
        scanSessionId: 'scan-1',
        familyId: 'family-1',
        shelfPhotoPath: 'temp/shelf.jpg',
        candidates: [],
        expiresAt: '2026-07-28T00:00:00Z',
      }),
      { status: 201, headers: { 'Content-Type': 'application/json' } },
    )
  }))

  render(
    <AuthSessionProvider
      initialSession={{
        status: 'authenticated',
        user: { id: 'user-1', displayName: 'Alice', email: 'alice@example.com' },
        family: { id: 'family-1', name: 'The Yans', memberCount: 1 },
      }}
    >
      <ScansPage />
    </AuthSessionProvider>,
  )

  await user.upload(screen.getByLabelText(/shelf photo/i), new File(['fake'], 'shelf.jpg', { type: 'image/jpeg' }))

  expect(await screen.findByText(/scanning/i)).toBeVisible()
})
```

- [ ] **Step 2: Run the page test and confirm it fails**

Run from `src/Librory.Web`: `npm run test:run -- src/pages/ScansPage.test.tsx`
Expected: fail because the page still renders the placeholder shell and has no upload flow.

- [ ] **Step 3: Implement the page and fetch helper**

```ts
export async function uploadShelfPhoto(file: File) {
  const formData = new FormData()
  formData.append('photo', file)

  const response = await fetch('/api/family/current/scan-sessions/uploads', {
    method: 'POST',
    credentials: 'include',
    body: formData,
  })

  if (!response.ok) {
    throw new Error('Shelf photo upload failed.')
  }

  return response.json()
}
```

```tsx
const [state, setState] = useState<'idle' | 'uploading' | 'scanning' | 'error'>('idle')
```

The page should:

1. Show a single image picker with camera-friendly capture behavior.
2. Call `uploadShelfPhoto` on file selection.
3. Show `Uploading...` while the request is in flight.
4. Switch to `Scanning...` when the API responds.
5. Show a retry message if the upload fails.

- [ ] **Step 4: Run the frontend test and make sure it passes**

Run from `src/Librory.Web`: `npm run test:run -- src/pages/ScansPage.test.tsx`
Expected: pass.

- [ ] **Step 5: Commit the web slice**

```bash
git add src/Librory.Web/src/pages/ScansPage.tsx src/Librory.Web/src/lib/scansApi.ts src/Librory.Web/src/pages/ScansPage.test.tsx
git commit -m "feat: add shelf photo upload UI"
```

---

### Task 4: Run the full feature verification pass

**Files:**
- None

**Interfaces:**
- Consumes: the completed API upload endpoint, cleanup service, and Scans page upload flow
- Produces: a tested end-to-end slice for Story 1

- [ ] **Step 1: Run the API test project**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj -v minimal`
Expected: all API tests pass, including upload and cleanup coverage.

- [ ] **Step 2: Run the Web test suite**

Run from `src/Librory.Web`: `npm run test:run`
Expected: all Web tests pass, including the new Scans page coverage.

- [ ] **Step 3: Fix anything surfaced by the full run and rerun the same commands**

If either test run fails, fix the issue in the smallest file set possible, then rerun the same command until both pass.
