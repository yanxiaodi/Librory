# Scan Purchase Follow-up Review Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resolve the latest PR review findings that can lose recognition state, permit unsafe concurrent writes, accept invalid purchase data, or restore an inconsistent scan flow.

**Architecture:** Preserve omitted correction values at the API/domain boundary, use PostgreSQL serializable transactions and optimistic concurrency for one-time state transitions, and keep persisted scan-session responses authoritative for resumed purchases. The Web flow will distinguish a fresh metadata search from an edited manual title and will surface continuation errors.

**Tech Stack:** .NET 10, ASP.NET Minimal APIs, EF Core/PostgreSQL, xUnit, React, TypeScript, Vitest.

**Spec:** `docs/superpowers/specs/2026-09-14-scan-purchase-recommendation-flow-design.md`

## Global Constraints

- Recognition rank remains separate from personalized recommendation score.
- Purchased candidates remain immutable through correction and discard paths.
- Version confirmation is one-time and must not overwrite a confirmed/shared catalog edition.
- Existing clients omitting newly added optional fields must retain persisted values.
- PostgreSQL check-then-write operations use `Serializable` isolation or an equivalent concurrency guard.

### Task 1: Preserve correction compatibility and complete scan-session responses

**Files:**
- Modify: `src/Librory.Api/Contracts/UpdateScanCandidateRequest.cs`
- Modify: `src/Librory.Domain/Models/ScanCandidate.cs`
- Modify: `src/Librory.Infrastructure/Scanning/ScanSessionService.cs`
- Modify: `src/Librory.Api/Endpoints/ScanSessionEndpoints.cs`
- Test: `tests/Librory.Api.Tests/ScanPurchaseEndpointsTests.cs`
- Test: `tests/Librory.Domain.Tests/ScanCandidateTests.cs`

**Interfaces:**
- `RecognitionRank` becomes nullable on correction requests; omitted values preserve the current rank while explicit zero remains valid.
- Correction responses load/map the current family so purchased candidate purchase data is not lost.
- Null metadata validation keys use the complete `metadataMatches[index]` path.

- [x] Write failing tests for omitted correction rank preservation, explicit zero rank, and correction response purchase data.
- [x] Run the focused tests and verify they fail for the current default-zero/null-family behavior.
- [x] Implement nullable rank propagation and family-aware correction response mapping.
- [x] Run focused API/domain tests and verify they pass.

### Task 2: Make one-time updates concurrency-safe

**Files:**
- Modify: `src/Librory.Api/Endpoints/BookEditionEndpoints.cs`
- Modify: `src/Librory.Infrastructure/Scanning/ScanSessionService.cs`
- Test: `tests/Librory.Api.Tests/ScanPurchaseEndpointsTests.cs`
- Test: `tests/Librory.Api.Tests/ScanSessionEndpointsTests.cs`

**Interfaces:**
- Edition confirmation serializes the provisional check and update and maps a concurrent loser to a non-success response.
- Candidate correction performs its pending/read-only check and save inside the same serializable transaction, mapping a stale purchase to a conflict/bad request.

- [x] Add failing concurrency tests using the existing PostgreSQL/integration test infrastructure.
- [x] Run the focused tests and verify the current endpoint/service permits the race or returns an unhandled exception.
- [x] Add serializable transactions and `DbUpdateConcurrencyException`/state-conflict translation without changing normal successful flows.
- [x] Run focused tests and the relevant existing purchase/correction tests.

### Task 3: Harden purchase validation and idempotency boundaries

**Files:**
- Modify: `src/Librory.Api/Endpoints/ScanPurchaseEndpoints.cs`
- Modify: `src/Librory.Infrastructure/Intake/ScanPurchaseService.cs`
- Test: `tests/Librory.Api.Tests/ScanPurchaseEndpointsTests.cs`

**Interfaces:**
- Purchase rejects overlong manual title/author, ISBN, format, condition, store, shelf location, and intake notes, plus invalid price/time/year ranges, before persistence.
- `ExistingEdition` and `SameWorkNewEdition` require a server-detected duplicate match.
- A concurrent duplicate `PurchaseRequestId` replays the committed purchase when it belongs to the same candidate and returns a client error only when it belongs elsewhere.
- Empty version confirmation requests are rejected when the provisional edition has no existing version values.

- [x] Add failing API tests for each boundary group, no-duplicate canonical selection, empty version confirmation, and concurrent/same-request replay behavior.
- [x] Run the focused tests and verify the current 500/accepted-input behavior.
- [x] Implement shared purchase-field validation, duplicate-match authorization, idempotency conflict reload, and non-empty version confirmation validation.
- [x] Run focused API tests and the existing purchase/retry suite.

### Task 4: Make Web metadata/purchase state explicit and resilient

**Files:**
- Modify: `src/Librory.Web/src/components/scans/BookRecognitionResults.tsx`
- Modify: `src/Librory.Web/src/pages/ScansPage.tsx`
- Test: `src/Librory.Web/src/components/scans/BookRecognitionResults.test.tsx`
- Test: `src/Librory.Web/src/pages/ScansPage.test.tsx`

**Interfaces:**
- Editing search text marks the candidate as requiring a fresh search; the purchase button is disabled until search completes, while initial no-match manual entry remains available.
- Search inputs are disabled while the provider request is in flight, and late responses cannot overwrite newer text.
- Continuation failures are surfaced to the page instead of being swallowed.
- Recognition rank is displayed as an unbounded recognition score or clamped consistently with the producer.
- Component tests cover purchase payload construction, duplicate confirmation retry, version confirmation, and intake-field selection.

- [x] Add failing Web tests for edited-title gating, continuation failure, purchase request construction, and version confirmation.
- [x] Run focused Vitest tests and verify the current UI permits stale/manual bypass or hides the error.
- [x] Implement explicit search-required state, request identity checks, continuation error state, and interaction coverage.
- [x] Run all Web tests, lint, and build.

### Task 5: Verify, commit, and push

- [x] Run `dotnet test Librory.sln --no-restore`.
- [x] Run `dotnet ef migrations has-pending-model-changes --project src/Librory.Infrastructure --startup-project src/Librory.Api --no-build` with the configured design-time connection string.
- [x] Run Web tests, lint, and build.
- [x] Run `git diff --check`, inspect status, commit the fixes, and push the existing PR branch.
