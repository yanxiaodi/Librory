# Story 16 Scan Recommendation Context API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a family-scoped backend scan context that selects one eligible target member, persists that choice, and records temporary language context without changing saved preferences.

**Architecture:** Keep target selection and language-context invariants in the scanning domain/application boundary. The infrastructure service resolves the active family member and recommendation profile before creating a scan, then passes a resolved context into `ScanSession.Create`. Candidate language is optional input metadata; the session derives a dominant language only when one language is unambiguously dominant and exposes mixed/unknown state for later scoring. API responses return target and language context but never profile notes.

**Tech Stack:** .NET 10, ASP.NET Minimal APIs, EF Core/PostgreSQL, existing Librory domain/application/infrastructure scanning services, xUnit integration tests.

## Global Constraints

- Select one target member for each scan and default the target to the current member.
- A supplied target must be active and belong to the current family; a different member requires an enabled family-visible recommendation profile unless the caller is an administrator.
- A missing target profile does not block recognition, metadata, or duplicate processing.
- A dominant scan language temporarily outranks saved language preferences; the saved profile is never mutated.
- Mixed-language scans retain per-candidate language information; unknown language remains visible and does not fail the scan.
- Do not add multiple targets, family-wide aggregate profiles, profile editing during a scan, AI recommendation generation, or frontend changes.

---

### Task 1: Add scan target and candidate language domain state

**Files:**
- Create: `src/Librory.Domain/Models/ScanLanguageContext.cs`
- Modify: `src/Librory.Domain/Models/ScanCandidate.cs`
- Modify: `src/Librory.Domain/Models/ScanSession.cs`
- Test: `tests/Librory.Domain.Tests/ScanSessionTests.cs`

**Interfaces:**
- Produces `ScanLanguageContext` with `PreferredLanguage? DominantLanguage` and `bool IsMixed`.
- Produces `ScanSession.TargetMemberId`, `ScanSession.TargetProfileAvailable`, `ScanSession.TargetProfileUsed`, `ScanSession.InferredLanguage`, and `ScanSession.HasMixedLanguages`.
- Produces `ScanCandidate.DetectedLanguage` and candidate creation/correction support for optional language metadata.

- [ ] **Step 1: Write failing domain tests**

Add tests proving a session stores its target and profile flags, a strict dominant language is recorded, mixed known languages produce no dominant language, unknown candidates do not fail, and candidate correction preserves its detected language unless a new value is supplied.

- [ ] **Step 2: Run the focused domain tests and confirm failure**

Run: `dotnet test tests/Librory.Domain.Tests/Librory.Domain.Tests.csproj --no-restore --filter ScanSession`

Expected: FAIL because target and language-context members do not exist.

- [ ] **Step 3: Implement the domain state**

Extend `ScanSession.Create` with target/context arguments while preserving an overload compatible with existing callers. Store target member id and profile availability/use flags. Add a method that recalculates context from current candidates after add/correction. Add optional `PreferredLanguage? detectedLanguage` to candidate creation and correction, and derive `DominantLanguage` only when the most common non-null language has a strict majority over other known languages; set `HasMixedLanguages` when more than one known language exists.

- [ ] **Step 4: Run focused tests and confirm success**

Run: `dotnet test tests/Librory.Domain.Tests/Librory.Domain.Tests.csproj --no-restore --filter ScanSession`

Expected: PASS, including existing scan-session tests.

- [ ] **Step 5: Commit the domain slice**

```bash
git add src/Librory.Domain/Models/ScanLanguageContext.cs src/Librory.Domain/Models/ScanCandidate.cs src/Librory.Domain/Models/ScanSession.cs tests/Librory.Domain.Tests/ScanSessionTests.cs
git commit -m "feat: add scan target and language context domain state"
```

### Task 2: Resolve target-member permissions and persist the new fields

**Files:**
- Modify: `src/Librory.Application/Scanning/ScanShelfRequest.cs`
- Modify: `src/Librory.Application/Scanning/ScanCandidateInput.cs`
- Modify: `src/Librory.Application/Scanning/ScanSessionRecorder.cs`
- Modify: `src/Librory.Infrastructure/Scanning/ScanSessionService.cs`
- Modify: `src/Librory.Infrastructure/Persistence/Configurations/ScanSessionConfiguration.cs`
- Modify: `src/Librory.Infrastructure/Persistence/Configurations/ScanCandidateConfiguration.cs`
- Generate: `src/Librory.Infrastructure/Persistence/Migrations/20260807160000_AddScanRecommendationContext.cs` and its generated designer/snapshot updates
- Test: `tests/Librory.Application.Tests/ScanOutputMappingTests.cs`
- Test: `tests/Librory.Api.Tests/LibroryDbContextModelTests.cs`
- Test: `tests/Librory.Api.Tests/ApiIntegrationTests.cs`

**Interfaces:**
- `ScanShelfRequest` accepts optional `Guid? TargetMemberId` and candidate `PreferredLanguage? DetectedLanguage`.
- `ScanSessionService.StartShelfScanAsync` resolves a `ScanTargetContext` from the current family and caller role before recording the session.
- `ScanSessionRecorder.Record` receives the resolved target/profile context and candidate language values.

- [ ] **Step 1: Write failing permission and persistence tests**

Add application tests for target defaulting and DTO mapping. Add API integration tests proving omitted target selects the caller, an administrator can target another active member, a normal member can target another member only when its profile is family-visible and enabled, foreign/inactive/private/disabled targets return 400 without creating a session, and a target with no profile still creates a session with profile flags false.

- [ ] **Step 2: Run focused tests and confirm failure**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~Scan"`

Expected: FAIL because the request, persistence, and response context are not implemented.

- [ ] **Step 3: Implement target resolution**

Load the current family members and target profile in `ScanSessionService`. Use the current member when `TargetMemberId` is omitted. Reject targets outside the family or inactive. For another member, allow only an admin or a profile with `ProfileVisibility.Family` and `UseInFamilyRecommendations == true`. Return a resolved context containing target id, display name, profile available, and profile used.

- [ ] **Step 4: Add EF configuration and migration**

Persist `TargetMemberId`, `TargetProfileAvailable`, `TargetProfileUsed`, `InferredLanguage`, and `HasMixedLanguages` on `scan_sessions`. Persist nullable `DetectedLanguage` on `scan_candidates`. Add the member foreign key and indexes needed for session-family queries. Generate the migration with EF tooling and update the snapshot; do not hand-author generated designer content.

- [ ] **Step 5: Run focused tests and confirm success**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~Scan"`

Expected: PASS with existing scan behavior and the new target permission cases.

- [ ] **Step 6: Commit the persistence and resolution slice**

```bash
git add src/Librory.Application/Scanning src/Librory.Infrastructure/Scanning src/Librory.Infrastructure/Persistence/Configurations src/Librory.Infrastructure/Persistence/Migrations tests/Librory.Application.Tests/ScanOutputMappingTests.cs tests/Librory.Api.Tests/LibroryDbContextModelTests.cs tests/Librory.Api.Tests/ApiIntegrationTests.cs
git commit -m "feat: resolve and persist scan recommendation targets"
```

### Task 3: Extend scan API contracts and response context

**Files:**
- Modify: `src/Librory.Api/Contracts/CreateScanSessionRequest.cs`
- Modify: `src/Librory.Api/Contracts/CreateScanCandidateRequest.cs`
- Modify: `src/Librory.Api/Contracts/ScanSessionResponse.cs`
- Modify: `src/Librory.Api/Contracts/ScanCandidateResponse.cs`
- Modify: `src/Librory.Application/Scanning/ScanSessionDto.cs`
- Modify: `src/Librory.Application/Scanning/ScanCandidateDto.cs`
- Modify: `src/Librory.Application/Scanning/ScanSessionDtoFactory.cs`
- Modify: `src/Librory.Api/Endpoints/ScanSessionEndpoints.cs`
- Test: `tests/Librory.Api.Tests/ApiIntegrationTests.cs`

**Interfaces:**
- Create request adds `Guid? TargetMemberId` and candidate adds `PreferredLanguage? DetectedLanguage`.
- Response adds `TargetMemberId`, `TargetMemberDisplayName`, `TargetProfileAvailable`, `TargetProfileUsed`, `InferredLanguage`, and `HasMixedLanguages`.
- Candidate response adds nullable `DetectedLanguage`.

- [ ] **Step 1: Write failing endpoint contract tests**

Post a scan with `targetMemberId` and candidate language values, then assert the response contains the selected member display name, target/profile flags, detected candidate language, and inferred/mixed context. Assert a scan without target still returns the current member.

- [ ] **Step 2: Run endpoint tests and confirm failure**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~Scan"`

Expected: FAIL because the JSON contracts and endpoint mapping do not include context.

- [ ] **Step 3: Implement contract and endpoint mapping**

Map the optional request fields into `ScanShelfRequest` and `ScanCandidateInput`. Project the resolved session context through `ScanSessionDtoFactory`, `ScanSessionResponse`, and candidate response mapping. Keep private profile notes out of every scan response.

- [ ] **Step 4: Run endpoint tests and confirm success**

Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~Scan"`

Expected: PASS with target selection, language context, and all existing scan endpoint tests.

- [ ] **Step 5: Commit the API contract slice**

```bash
git add src/Librory.Api/Contracts src/Librory.Api/Endpoints/ScanSessionEndpoints.cs src/Librory.Application/Scanning tests/Librory.Api.Tests/ApiIntegrationTests.cs
git commit -m "feat: expose scan recommendation context api"
```

### Task 4: Document and validate Story 16 backend delivery

**Files:**
- Create: `docs/devlog/2026-08-07-story-16-scan-recommendation-context-api.md`
- Modify: `docs/api-reference.md`

- [ ] **Step 1: Document the new request/response fields and authorization rules**

Document omitted-target defaulting, eligible alternate targets, no-profile behavior, mixed/unknown language semantics, and the fact that profile notes are never returned.

- [ ] **Step 2: Run the complete solution tests**

Run: `dotnet test Librory.sln --no-restore`

Expected: PASS with zero failures.

- [ ] **Step 3: Run the complete solution build**

Run: `dotnet build Librory.sln --no-restore`

Expected: 0 warnings and 0 errors.

- [ ] **Step 4: Run diff and workspace checks**

```bash
git diff --check
git status --short
git diff main...HEAD --stat
```

Expected: only Story 16 backend source, tests, migration, API docs, devlog, and implementation plan files are present.

- [ ] **Step 5: Commit the documentation and plan**

```bash
git add docs/api-reference.md docs/devlog/2026-08-07-story-16-scan-recommendation-context-api.md docs/superpowers/plans/2026-08-07-story-16-scan-recommendation-context-api.md
git commit -m "docs: record story 16 scan recommendation context"
```
