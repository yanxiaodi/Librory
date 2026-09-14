# Scan-to-Purchase Recommendation Flow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete the reliable first-slice journey from a multi-book shelf scan to one-by-one family-library purchases, while keeping recognition quality, duplicate detection, metadata provenance, purchase ownership, and future AI recommendation state separate.

**Architecture:** Keep scan-session orchestration in the existing Application/Infrastructure scanning services. Add a dedicated purchase application contract and Infrastructure implementation. The purchase implementation opens the outer PostgreSQL transaction, invokes a transaction-participating canonical metadata importer, evaluates family duplicates, creates the copy, and marks the candidate purchased as one atomic operation. Use a fresh DbContext per retry attempt and retry the complete operation only for PostgreSQL serialization failure (`40001`) or deadlock (`40P01`). Store the normalized metadata-match snapshot as stable JSONB on `ScanCandidate`; expose it through API DTOs without exposing persistence entities. Extend the existing Web scan review surface with metadata selection, correction/re-search, owner selection, duplicate resolution, and per-candidate purchase state.

**Tech Stack:** .NET 9, ASP.NET Core minimal APIs, EF Core with Npgsql/PostgreSQL, existing domain model and metadata-provider abstractions, React/TypeScript/Vite, Vitest/Testing Library, xUnit integration tests.

**Spec:** `docs/superpowers/specs/2026-09-14-scan-purchase-recommendation-flow-design.md`

## Global Constraints

- Do not add Wishlist behavior, AI scoring, recommendation-job polling, purchase location, quantities, or batch checkout in this slice.
- Replace persisted `RecommendationScore` semantics with recognition quality (`RecognitionRank`); do not display recognition quality as a personalized recommendation score. Preserve a nullable/future-ready recommendation result only if the existing model requires it, and keep it unused in this slice.
- A scan session contains many candidates, but each candidate can create at most one purchased copy. A second copy requires a new scan or manual intake. Purchased candidates stay in the session as read-only records; other candidates remain purchasable.
- The authenticated caller must be an active member of the current family. `OwningMemberId` may refer to any same-family member, including a deactivated member. `PurchasedByMemberId` is always derived from the authenticated caller and is never accepted from the request.
- Keep the seven-day default session retention and never extend expiration after purchase. Cleanup may remove temporary scan data but must not remove a created `BookCopy`.
- Purchase time defaults to `DateTimeOffset.UtcNow` and remains editable. Keep existing optional store, price, condition, shelf-location, and intake-note fields; do not add purchase location fields.
- A no-duplicate result is recorded as `ConfirmedUnique`; a duplicate requires explicit client confirmation and is recorded as `ConfirmedDuplicate`. Duplicate detection warns but does not block a confirmed purchase. `Unchecked` is invalid for a completed purchase.
- Metadata snapshots use one JSONB column with an explicit versioned DTO and deterministic serialization. The snapshot must survive session reload and contain enough provider/manual data to reproduce the selected match.
- Provisional editions use explicit `BookEdition.IsProvisional`; never create an editionless `BookCopy`. Manual fallback creates a `Manual`-provenance provisional work/edition.
- Provider/manual provenance must remain field-specific: user corrections are not silently overwritten by a later provider refresh.
- Metadata import must not begin or commit its own transaction when called by purchase. One DbContext and one Npgsql transaction cover import, duplicate evaluation, copy creation, and candidate-state update. `SaveChangesAsync` does not commit.
- On `40001` or `40P01`, discard the failed context/transaction and retry the entire purchase operation with a fresh context, at most two retries. Never continue using a failed transaction and never leave canonical artifacts from a failed attempt.
- Use `apply_patch` for source edits. After each task, run the smallest relevant tests; before completion run the complete .NET test suite, Web tests, lint/build, migration/model checks, and the manual mobile-flow verification described in the spec.

---

## Task 1: Establish domain state and persistence shape

### 1.1 Add the domain concepts before changing callers

- [ ] Add `PurchaseStatus` with `Pending` and `Purchased` values under `src/Librory.Domain/Models/`.
- [ ] Add a versioned `ScanCandidateMetadataSnapshot` contract under `src/Librory.Application/Scanning/` containing provider/source id, title, authors, subtitle, publication data, ISBNs, cover/link fields, and source/provenance fields. Keep this contract serialization-only; do not expose EF entities.
- [ ] Change `src/Librory.Domain/Models/ScanCandidate.cs` so the persisted recognition field is `RecognitionRank` (non-negative, deterministic from recognition order), remove the recommendation-score validation from the scan-candidate workflow, and add:
  - `MetadataMatchesJson` as nullable JSON text managed by the application serializer;
  - `PurchaseStatus`, `PurchasedBookCopyId`, `PurchaseRequestId`, and `PurchasedAt`;
  - a method that replaces the metadata snapshot and resets stale review/duplicate state;
  - a method that transitions exactly once from pending to purchased and rejects a second purchase or mutation of a purchased candidate.
- [ ] Add `PurchasedByMemberId` to `src/Librory.Domain/Models/BookCopy.cs`, retain `MemberId` as the owner, and update `BookCopy.Create` to require both owner and purchaser members from the same family. Allow historical rows to keep a null purchaser through a private EF-compatible setter/backward-compatible nullable property, while all new creation paths pass the authenticated purchaser.
- [ ] Add explicit `IsProvisional` to `src/Librory.Domain/Models/BookEdition.cs`, with a domain method that confirms a provisional edition only when its supplied version fields are valid. Preserve existing metadata provenance when setting corrected manual fields.
- [ ] Add `UsePrivateNotesInFamilyRecommendations` defaulting to `false` to `src/Librory.Domain/Models/RecommendationProfile.cs` and its change tracking. Enforce profile-owner-only read/update behavior in the application contract; administrators must not gain note/consent access.

### 1.2 Update EF mappings and create the migration

- [ ] Update `BookCopyConfiguration.cs`, `BookEditionConfiguration.cs`, `ScanCandidateConfiguration.cs`, and `RecommendationProfileConfiguration.cs` for the new columns, enum conversions, JSONB type, nullable historical purchaser, indexes, and the candidate purchase-request uniqueness needed for idempotency.
- [ ] Update all affected model snapshots and add one migration under `src/Librory.Infrastructure/Persistence/Migrations/` that renames/removes the old recommendation-score column as appropriate, adds purchaser/provisional/purchase-state/consent fields, creates the JSONB column, and creates only the constraints required by the design. Preserve existing data and default existing candidates to `Pending`.
- [ ] Verify the migration can apply to a database containing the initial schema and that the generated model snapshot agrees with the configurations.

### 1.3 Lock behavior with domain tests

- [ ] Add/update Domain tests for recognition rank bounds/order, pending-to-purchased one-way transition, purchased-candidate immutability, provisional-to-confirmed editions, owner/purchaser family validation, and profile consent default plus owner-only mutation.
- [ ] Run the focused Domain test project and inspect the migration diff before moving to API/application work.

## Task 2: Carry metadata matches through scan sessions

### 2.1 Define stable application/API contracts

- [ ] Update `ScanCandidateInput.cs`, `CorrectionRequest.cs`, `ScanCandidateDto.cs`, `ScanCandidateDtoFactory.cs`, and `ScanSessionDtoFactory.cs` to use `RecognitionRank`, metadata matches, and purchase state instead of `RecommendationScore`.
- [ ] Add `MetadataMatchSnapshot`/`MetadataMatchSelection` request and response records under `src/Librory.Application/Scanning/` and `src/Librory.Api/Contracts/`. Include a schema version and stable field names; reject malformed or oversized snapshots at the API boundary.
- [ ] Add one serializer in the application layer using explicit `JsonSerializerOptions` and round-trip tests for provider results, manual results, corrected fields, and empty-match fallback.

### 2.2 Persist and reload snapshots

- [ ] Update `ScanSessionRecorder.cs` and `ScanSessionService.cs` to write the normalized snapshot JSON when candidates are created or corrected, and to replace the snapshot on explicit metadata re-search while resetting stale duplicate/review state.
- [ ] Update `ScanSessionEndpoints.cs` request binding and response mapping for candidate creation, correction, discard, reload, and latest-session continuation. Ensure purchased candidates are returned, cannot be discarded, and cannot be corrected.
- [ ] Preserve the existing scan-target validation for creating a scan session; do not use that active/eligible filter for purchase-owner selection.

### 2.3 Test session persistence behavior

- [ ] Add application/API tests proving snapshots survive a save/reload round trip through the JSONB column, correction replaces old matches, purchased candidates remain visible/read-only, and expired sessions are not returned.
- [ ] Run the Application and API test projects before adding purchase behavior.

## Task 3: Refactor canonical metadata import for caller-owned transactions

### 3.1 Introduce transaction-participating import options/results

- [ ] Extend `BookMetadataCandidate`, `BookMetadataImportResult`, and `IBookMetadataImportService` with an import-options record that supports selected edition fields, `AllowProvisionalEdition`, and manual fallback while preserving provider source ids and field provenance.
- [ ] Refactor `BookMetadataImportService.cs` so its core operation only loads/creates/tracks canonical work and edition entities and does not call `BeginTransactionAsync`, commit, or rely on an independent save. Existing public metadata import remains supported by an endpoint-owned transaction wrapper.
- [ ] Make ISBN reuse/conflict handling PostgreSQL-safe: query existing ISBNs within the caller transaction, rely on the unique constraint for the final race, and translate a conflicting canonical edition into a retryable/import conflict result rather than leaving a partial work.
- [ ] Ensure every purchase path resolves a concrete edition. Provider failure or no-match manual input creates a manual provisional work/edition; provider data with missing version fields creates a provisional edition when requested; normal provider import does not regress to an editionless work.
- [ ] Apply provider provenance only to provider-owned fields and manual provenance to user-corrected fields. Add a focused import method or options path for “same existing edition,” “same work/new edition,” and “new work/new edition.”

### 3.2 Update existing metadata endpoint and tests

- [ ] Update `BookMetadataEndpoints.cs` to own the transaction for standalone imports, call the refactored import service, save once, commit once, and map the existing response contract.
- [ ] Update import tests for ISBN reuse, no-ISBN provisional edition creation, manual fallback, provenance preservation, and rollback when save/commit fails.
- [ ] Run the metadata and API tests and confirm the standalone import path still behaves as before except for the explicit provisional-edition correction.

## Task 4: Implement the atomic, idempotent scan-candidate purchase service

### 4.1 Define the purchase boundary

- [ ] Add `ScanPurchaseRequest`, `ScanPurchaseResult`, `DuplicateResolution`, and `IScanPurchaseService` under `src/Librory.Application/Intake/` or a dedicated `Purchasing` namespace. The request must contain `PurchaseRequestId`, scan/candidate ids, selected normalized metadata or manual metadata, owner id, optional duplicate resolution/existing work or edition ids, optional purchase fields, and editable purchase time; it must not contain purchaser id or duplicate status authority.
- [ ] Define the result with created/reused work and edition, copy id, owner id, purchaser id, final duplicate status, provisional state, and idempotency information so the endpoint can return a stable response for retries.

### 4.2 Implement the transaction and retry loop

- [ ] Add `ScanPurchaseService` in Infrastructure and register it in `DependencyInjection.cs`. Use a DbContext factory or fresh scoped DbContext per attempt so a failed PostgreSQL transaction is never reused.
- [ ] Implement a bounded loop of one initial attempt plus two retries. Each attempt must:
  1. load the current family, active caller, non-expired session, and candidate;
  2. return the existing result when the same candidate/request idempotency key already completed;
  3. reject a different request for an already-purchased candidate;
  4. validate that the owner is any same-family member, including deactivated members;
  5. resolve the selected metadata/manual fallback and duplicate-resolution branch;
  6. invoke the transaction-participating importer;
  7. rerun family-wide duplicate detection against the resolved edition;
  8. require explicit duplicate confirmation when a duplicate exists and normalize no-duplicate to `ConfirmedUnique`;
  9. create `BookCopy` with `MemberId = owner` and `PurchasedByMemberId = caller`;
  10. save the copy, candidate purchase state, and idempotency result within one transaction, then commit.
- [ ] Detect PostgreSQL SQLSTATE `40001` and `40P01` from the provider exception chain. On either code dispose the transaction/context and retry the complete operation. After the third failed attempt return a retryable application error with no successful copy id.
- [ ] Keep canonical import and copy creation in the same transaction so a failed purchase cannot leave an orphan work/edition. Do not add nested transactions or independent `SaveChangesAsync` calls that commit implicitly.

### 4.3 Cover purchase invariants with integration tests

- [ ] Add API/application integration tests for active caller purchasing for another active member, a deactivated member, and an admin/non-admin; reject unauthenticated and foreign-family owners; derive purchaser from auth; reject a second purchase; and return the original result for repeated `PurchaseRequestId`.
- [ ] Add tests for no-duplicate auto-confirmation, duplicate confirmation requirement, same-edition reuse, same-work/new-edition, new-work/new-edition, manual/provisional fallback, and metadata/import failure rollback.
- [ ] Add a retry test that injects `40001`/`40P01` on the first attempt and verifies the second attempt creates exactly one copy; add an exhausted-retry test verifying the response is retryable and the database contains no partial purchase.
- [ ] Run the full API integration suite with PostgreSQL and inspect rows for purchaser, owner, duplicate status, candidate state, and idempotency data.

## Task 5: Expose the purchase endpoint and profile privacy rule

### 5.1 Add dedicated API contracts and route

- [ ] Add `ConfirmScanPurchaseRequest`, `DuplicateResolutionRequest`, and `ScanPurchaseResponse` under `src/Librory.Api/Contracts/`. Validate required owner, selected/manual metadata, purchase request id, editable purchase time, and duplicate confirmation shape at the boundary.
- [ ] Add `ScanPurchaseEndpoints.cs` with `POST /api/family/current/scan-sessions/{scanSessionId}/candidates/{candidateId}/purchase`, require authentication, resolve current family context, delegate all business decisions to `IScanPurchaseService`, and map not-found/conflict/validation/retryable outcomes to stable HTTP responses.
- [ ] Register the endpoint in `Program.cs` and reuse `BookCopyResponseFactory`/canonical metadata response factories without returning EF entities or private notes.
- [ ] Update `RecommendationProfileEndpoints.cs` and its application DTO/factory so only the profile owner can read or change private notes and `UsePrivateNotesInFamilyRecommendations`; administrators cannot access those fields. Keep structured preference family-sharing behavior separate and unchanged.

### 5.2 Verify the public contract

- [ ] Add endpoint tests for route binding, auth, family isolation, idempotent replay, duplicate decision errors, provisional/manual responses, and purchaser non-spoofing.
- [ ] Add privacy tests proving the consent field defaults false and is hidden/rejected for admins and other members while the owner can read and update it.
- [ ] Update `docs/api-reference.md` and `docs/frontend-integration-guide.md` with the purchase route, request/response examples, idempotency behavior, duplicate-resolution choices, session continuation, and owner/purchaser semantics. Record progress in `docs/story-progress.md` if that file is the project progress ledger.

## Task 6: Complete the Web scan review and purchase flow

### 6.1 Add typed API clients and state mapping

- [ ] Update the scan API client/types used by `ScansPage.tsx`, `BookRecognitionResults.tsx`, and existing recognition helpers to remove recommendation-score labels, expose recognition rank/evidence, metadata snapshots, duplicate status, provisional state, and purchase state.
- [ ] Add metadata search/research client helpers that send a corrected title/author without re-uploading the photo, and add the purchase client helper that always generates/persists a stable `purchaseRequestId` for a submission attempt.
- [ ] Add a family-member list path for purchase owners that returns all same-family members, including deactivated members. Keep the existing eligible-active list for scan target selection.

### 6.2 Build candidate review and purchase interaction

- [ ] Extend `BookRecognitionResults.tsx` or extract a focused `ScanCandidatePurchaseCard.tsx`/`PurchaseBookDialog.tsx` to render recognition match quality/evidence, metadata fields and matches, default top-ranked selection, explicit metadata change, version-unconfirmed label, duplicate warning, owner selector defaulted to scan target, purchase time defaulted to now, and optional intake fields.
- [ ] Add the duplicate decision UI with exactly three resolution choices: existing edition, same work/new edition, or new work/edition. Require the choice only when the server reports a duplicate; never silently block the purchase.
- [ ] Disable submit while saving, preserve form state and selected metadata on errors, show the returned copy/owner/purchaser on success, and make only the purchased candidate read-only. Keep all other candidates purchasable.
- [ ] Make correction/re-search replace the candidate snapshot and clear stale duplicate/review state. Do not implement AI polling or recommendation sorting in this task.

### 6.3 Continue sessions from Home and Scans

- [ ] Update `ScansPage.tsx` to load the latest non-expired session when entered through continuation, render pending and purchased candidates together, and retain the existing photo/recognition polling behavior.
- [ ] Update `HomePage.tsx` to show “Continue latest scan” only when a non-expired session exists and navigate to the scan review route. Do not extend expiration after a purchase.
- [ ] Add/update Web tests for recognition-vs-recommendation labels, metadata correction/re-search, default and changed match selection, all-family owner selection, editable purchase time, the three duplicate branches, per-candidate read-only state, remaining candidates, reload persistence, idempotent replay, and preserved form state after errors.
- [ ] Run Web unit tests, lint, and production build after the UI task.

## Task 7: End-to-end verification and handoff

- [ ] Apply the migration to a disposable PostgreSQL database and run the complete .NET solution tests: Domain, Application, and API. Confirm zero failures and investigate any warning introduced by this work.
- [ ] Run `npm run test:run`, `npm run lint`, and `npm run build` in `src/Librory.Web`.
- [ ] Run the existing mobile shelf-photo flow manually: recognize multiple books, correct and re-search one title, purchase several different candidates, assign copies to active and deactivated members, exercise duplicate resolution branches, create a manual/provisional edition, reload from Home, and verify owner/purchaser/provenance/duplicate status in the library.
- [ ] Inspect `git diff --check`, migration SQL/model snapshot, and the final changed-file list. Confirm no purchase-location fields, AI score, wishlist behavior, nested transaction, or client-supplied purchaser was added.
- [ ] Commit each coherent task with a focused message, then run the final verification commands again on the branch before reporting the branch name, commits, tests, and any remaining follow-up.
