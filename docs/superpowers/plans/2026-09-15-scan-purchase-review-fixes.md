# Scan Purchase Review Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans (recommended) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resolve the remaining PR review findings that can corrupt scan/purchase state, expose unsafe edition mutation, or make provisional-edition confirmation unavailable.

**Architecture:** Keep canonical edition version changes server-authorized and provisional-only. Make scan-session responses carry enough persisted purchase/version data for the Web UI to restore the same state after reload. Keep recognition candidates associated by stable candidate IDs and pass the active job's candidates explicitly when the initial session is created.

**Tech Stack:** .NET 10, ASP.NET Minimal APIs, EF Core with PostgreSQL, xUnit, React, TypeScript, Vitest.

**Spec:** `docs/superpowers/specs/2026-09-14-scan-purchase-recommendation-flow-design.md`

## Global Constraints

- PostgreSQL transactions that protect check-then-create operations use `Serializable` isolation.
- A purchased scan candidate is immutable through scan correction and discard paths.
- Recognition rank is not persisted as a personalized recommendation score.
- A book edition is a shared catalog entity; only the provisional edition created for the current family's purchase flow can be version-confirmed.
- Existing feature behavior and the current API response shapes remain backward compatible where possible; new response fields are optional.

### Task 1: Protect and validate provisional edition version updates

**Files:**
- Modify: `src/Librory.Domain/Models/BookEdition.cs`
- Modify: `src/Librory.Api/Endpoints/BookEditionEndpoints.cs`
- Modify: `src/Librory.Api/Contracts/UpdateBookEditionVersionRequest.cs`
- Test: `tests/Librory.Domain.Tests/BookEditionMetadataTests.cs`
- Test: `tests/Librory.Api.Tests/ScanPurchaseEndpointsTests.cs`

**Interfaces:**
- `BookEdition.UpdateVersion` preserves fields whose request values are omitted and records a `Manual` publication-year provenance when a year is supplied.
- `PUT /api/family/current/book-editions/{bookEditionId}/version` accepts only an active current member and a provisional edition referenced by a copy in the current family.

- [x] **Step 1: Write failing domain tests** for preserving an omitted ISBN/year and for setting manual publication-year provenance when only format/year is updated.
- [x] **Step 2: Run** `dotnet test tests/Librory.Domain.Tests/Librory.Domain.Tests.csproj --no-restore --filter FullyQualifiedName~BookEditionMetadataTests`; verify the new tests fail against the current overwrite/no-provenance behavior.
- [x] **Step 3: Implement** nullable update semantics, normalization, and manual provenance in `BookEdition.UpdateVersion` without clearing omitted values.
- [x] **Step 4: Run** the focused domain tests and verify they pass.
- [x] **Step 5: Write failing API tests** for deactivated-member rejection, confirmed-edition rejection, and overlong ISBN/format returning validation instead of 500.
- [x] **Step 6: Run** the focused API tests and verify they fail for the current endpoint authorization/validation.
- [x] **Step 7: Implement** active-member and provisional-edition checks, field-length validation, publication-year validation, and safe exception translation.
- [x] **Step 8: Run** the focused API tests and verify they pass.

### Task 2: Harden purchase and scan-session API boundaries

**Files:**
- Modify: `src/Librory.Api/Validation/ApiValidation.cs`
- Modify: `src/Librory.Api/Endpoints/ScanPurchaseEndpoints.cs`
- Modify: `src/Librory.Api/Endpoints/ScanSessionEndpoints.cs`
- Modify: `src/Librory.Infrastructure/Intake/ScanPurchaseService.cs`
- Modify: `src/Librory.Infrastructure/Scanning/ScanSessionService.cs`
- Test: `tests/Librory.Api.Tests/ScanPurchaseEndpointsTests.cs`
- Test: `tests/Librory.Api.Tests/ScanSessionEndpointsTests.cs`

**Interfaces:**
- Shared metadata validation rejects blank, overlong, and oversized nested metadata before database writes.
- Purchase loading uses `AsSplitQuery`; replay responses include the complete work-edition collection.
- Serializable retry exhaustion remains mapped to a structured retryable 503.

- [x] **Step 1: Add failing API tests** for oversized selected metadata, invalid nested metadata in session creation/correction, and retry exhaustion.
- [x] **Step 2: Run** the focused API test filters and verify the new tests fail with the current 500/accepted-input behavior.
- [x] **Step 3: Implement** shared bounded metadata validation and use it in purchase and scan-session endpoints, including author entries and match count/field limits.
- [x] **Step 4: Add** `AsSplitQuery()` to the purchase family load and ensure replay loads `BookWork.Editions`.
- [x] **Step 5: Run** the focused API tests and verify they pass.

### Task 3: Make persisted scan-session state complete and durable

**Files:**
- Modify: `src/Librory.Application/Scanning/ScanCandidateDto.cs`
- Modify: `src/Librory.Api/Contracts/ScanCandidateResponse.cs`
- Modify: `src/Librory.Api/Endpoints/ScanSessionEndpoints.cs`
- Modify: `src/Librory.Web/src/lib/scansApi.ts`
- Modify: `src/Librory.Web/src/pages/ScansPage.tsx`
- Modify: `src/Librory.Web/src/components/scans/BookRecognitionResults.tsx`
- Test: `tests/Librory.Api.Tests/ScanPurchaseEndpointsTests.cs`
- Test: `src/Librory.Web/src/pages/ScansPage.test.tsx`

**Interfaces:**
- A scan candidate response can restore purchased copy/edition and provisional-version data after reload.
- The Web discard handler calls the existing DELETE endpoint once the session exists.
- Initial session persistence accepts an explicit current-job candidate list; retry persistence can reuse edited state.

- [x] **Step 1: Write failing API/Web tests** for purchased candidate response data, durable discard, reload of provisional purchase state, and a second scan persisting only its own candidates.
- [x] **Step 2: Run** the focused tests and verify they fail because the current response lacks edition data, discard is local-only, and the persistence callback captures old state.
- [x] **Step 3: Implement** optional purchased edition fields in the scan-session response and populate them with a query that safely loads the purchased copy/edition/work.
- [x] **Step 4: Implement** the DELETE client call and await it before removing a persisted candidate from the visible list.
- [x] **Step 5: Refactor** `persistScanSession` to accept an explicit candidate override for the initial save and use the edited candidate list only for retries.
- [x] **Step 6: Restore** purchase response/version state when building a continuation job.
- [x] **Step 7: Run** focused API and Web tests and verify they pass.

### Task 4: Fix Web purchase review state transitions

**Files:**
- Modify: `src/Librory.Web/src/components/scans/BookRecognitionResults.tsx`
- Modify: `src/Librory.Web/src/pages/ScansPage.tsx`
- Test: `src/Librory.Web/src/components/scans/BookRecognitionResults.test.tsx`
- Test: `src/Librory.Web/src/pages/ScansPage.test.tsx`

**Interfaces:**
- After purchase, the result summary and provisional-version confirmation remain rendered while pending purchase inputs are hidden.
- Editing search text clears stale metadata matches and blocks purchase until a new search/manual selection is available.
- Changing the selected metadata or duplicate match clears incompatible duplicate confirmation state.

- [x] **Step 1: Write failing component tests** for provisional confirmation remaining visible after purchase, stale metadata being cleared on title edit, and duplicate state clearing on metadata selection change.
- [x] **Step 2: Run** the focused Vitest tests and verify they fail against the current conditional rendering/state handlers.
- [x] **Step 3: Implement** the separated pending-controls/result-summary rendering and stable candidate-to-persisted mapping by candidate ID.
- [x] **Step 4: Implement** stale metadata/duplicate state invalidation and preserve awaited metadata-save behavior.
- [x] **Step 5: Run** focused Web tests and verify they pass.

### Task 5: Complete verification and review handoff

**Files:**
- Modify: `docs/api-reference.md` only if response or validation contracts changed.
- Modify: `docs/frontend-integration-guide.md` only if the restored provisional state requires new integration guidance.

- [x] **Step 1: Run** `dotnet test Librory.sln --no-restore`.
- [x] **Step 2: Run** Web `npm run test:run -- --run`, `npm run lint`, and `npm run build` from `src/Librory.Web`.
- [x] **Step 3: Run** `dotnet ef migrations has-pending-model-changes --project src/Librory.Infrastructure --startup-project src/Librory.Api --no-build`.
- [x] **Step 4: Run** `git diff --check` and inspect `git status --short`.
- [ ] **Step 5: Commit the verified changes and push the existing PR branch.
