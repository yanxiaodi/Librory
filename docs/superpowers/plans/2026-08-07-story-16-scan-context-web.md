# Story 16 Scan Context Web Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let users select an eligible family member before a shelf scan and persist/display the Story 16 scan recommendation context after recognition completes.

**Architecture:** Keep the existing book-recognition upload and polling flow. When recognition succeeds, map its candidates into a typed scan-session create request containing the selected `targetMemberId`; render the returned scan context above the recognition results. Use the existing family member API for selection and keep recognition results available if session persistence fails.

**Tech Stack:** React 19, TypeScript, React Testing Library, Vitest, existing `familyApi`, `bookRecognitionApi`, `scansApi`, Tailwind utility classes.

## Global Constraints

- One scan has exactly one target member; multiple members require separate scans.
- The current member is always the default and fallback target.
- Only active members eligible for family recommendations may be selected, apart from the current member fallback.
- Never request or render private recommendation profile notes.
- Preserve existing recognition upload, polling, and error behavior.
- Run web tests, lint, and production build before completion.

---

### Task 1: Add typed scan-session API transport

**Files:**
- Modify: `src/Librory.Web/src/lib/scansApi.ts`
- Create: `src/Librory.Web/src/lib/scansApi.test.ts`

**Interfaces:**
- `CreateScanCandidateRequest` contains `displayTitle`, `confidenceLabel`, optional `author`, `recommendationScore`, `isAlreadyOwned`, `duplicateMessage`, and optional numeric `detectedLanguage`.
- `CreateScanSessionRequest` contains `shelfPhotoPath`, optional `candidates`, and optional `targetMemberId`.
- `ScanSessionResponse` contains the API response fields `scanSessionId`, `familyId`, `shelfPhotoPath`, `candidates`, `expiresAt`, `targetMemberId`, `targetMemberDisplayName`, `targetProfileAvailable`, `targetProfileUsed`, optional `inferredLanguage`, and `hasMixedLanguages`.
- `createScanSession(input: CreateScanSessionRequest): Promise<ScanSessionResponse>` POSTs JSON to `/api/family/current/scan-sessions` with `credentials: 'include'`.

- [ ] **Step 1: Write the failing transport tests**

Test that `createScanSession` sends the exact target member and candidate fields as JSON, uses `POST`, includes credentials, returns the decoded response, and throws on a non-2xx response.

- [ ] **Step 2: Run the focused test and verify it fails**

Run: `npm run test:run -- src/lib/scansApi.test.ts`

Expected: FAIL because `createScanSession` and the typed request/response contracts do not exist.

- [ ] **Step 3: Implement the minimal transport**

Add the interfaces and implement:

```ts
export async function createScanSession(input: CreateScanSessionRequest): Promise<ScanSessionResponse> {
  const response = await fetch('/api/family/current/scan-sessions', {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })

  if (!response.ok) throw new Error(`Scan session creation failed (${response.status}).`)
  return response.json() as Promise<ScanSessionResponse>
}
```

- [ ] **Step 4: Run the focused test and verify it passes**

Run: `npm run test:run -- src/lib/scansApi.test.ts`

Expected: PASS.

- [ ] **Step 5: Commit the transport slice**

```bash
git add src/Librory.Web/src/lib/scansApi.ts src/Librory.Web/src/lib/scansApi.test.ts
git commit -m "feat: add scan session web api client"
```

### Task 2: Add target-member selection and scan-context orchestration

**Files:**
- Modify: `src/Librory.Web/src/pages/ScansPage.tsx`
- Modify: `src/Librory.Web/src/pages/ScansPage.test.tsx`
- Use: `src/Librory.Web/src/lib/familyApi.ts` (`listMembers`, `FamilyMember`)
- Use: `src/Librory.Web/src/auth/AuthSessionContext.tsx` (`family.memberId`, `family.memberDisplayName`)

**Interfaces:**
- `ScansPage` loads `listMembers()` and maintains a selected target member id.
- The recognition job response remains the source for candidate display.
- A successful recognition response is mapped into `CreateScanSessionRequest`; the returned `ScanSessionResponse` is stored as `scanSession`.

- [ ] **Step 1: Add failing page tests for member selection and defaulting**

Extend `ScansPage.test.tsx` with mocked member data for current member Alice and eligible member Bob. Assert the selector defaults to Alice, selecting Bob changes the value, and the member list request is made with credentials through `listMembers`.

- [ ] **Step 2: Run the focused test and verify it fails**

Run: `npm run test:run -- src/pages/ScansPage.test.tsx`

Expected: FAIL because the page has no target-member selector or member loading.

- [ ] **Step 3: Implement loading, eligibility, and selection**

On mount, call `listMembers()`. Keep members where `isActive` is true and either `memberId === currentMemberId` or `canUseForFamilyRecommendations === true`. Initialize the selected id to `currentMemberId`; if loading fails, render a non-blocking message and retain the current member option. Reset an invalid selected id to the current member when the member list changes.

Render a labeled native `<select>` before the photo picker. The label should explain the action, for example `Scan for member`, and each option should use the member display name.

- [ ] **Step 4: Add failing tests for persistence and context display**

Update the successful recognition test so the mocked completed job is followed by a successful `POST /api/family/current/scan-sessions`. Assert its JSON includes:

```json
{
  "shelfPhotoPath": "/tmp/Librory/scan-uploads/shelf.jpg",
  "targetMemberId": "member-2",
  "candidates": [{ "displayTitle": "Dune", "confidenceLabel": "DUNE" }]
}
```

Return a scan response with `targetMemberDisplayName: "Bob"`, `targetProfileAvailable: true`, `targetProfileUsed: true`, `inferredLanguage: 0`, and `hasMixedLanguages: false`; assert those details are visible. Add a test that a failed session POST keeps `Dune` visible and shows a retry action.

- [ ] **Step 5: Run the focused tests and verify the new tests fail**

Run: `npm run test:run -- src/pages/ScansPage.test.tsx`

Expected: the new persistence/context assertions fail while the existing recognition-only tests may need their fetch mocks updated for the new session request.

- [ ] **Step 6: Implement recognition-to-session mapping**

After a recognition job reaches status `2`, create a scan session once per job. Map each recognized candidate using the first metadata author, the rank normalized to a `0..1` recommendation score, the evidence text as `confidenceLabel`, and `en`/`zh` metadata languages to the backend enum values used by the project. Keep unknown languages undefined. Pass the selected target id.

Track the persistence state separately from recognition state so the recognition result remains rendered while the session request is pending or failed. Add a retry handler that reuses the completed job and selected target without uploading the photo again.

Render a compact context card with the target display name, profile-used status, inferred language label, and a mixed-language note when `hasMixedLanguages` is true. Do not render profile notes.

- [ ] **Step 7: Run the focused tests and verify they pass**

Run: `npm run test:run -- src/pages/ScansPage.test.tsx`

Expected: PASS for existing recognition behavior plus member selection, session payload, context display, and retry behavior.

- [ ] **Step 8: Commit the page slice**

```bash
git add src/Librory.Web/src/pages/ScansPage.tsx src/Librory.Web/src/pages/ScansPage.test.tsx
git commit -m "feat: add scan target member context to web flow"
```

### Task 3: Verify the complete web slice

**Files:**
- No source changes expected; inspect the files from Tasks 1-2.

- [ ] **Step 1: Run the complete frontend test suite**

Run from `src/Librory.Web`: `npm run test:run`

Expected: all tests pass.

- [ ] **Step 2: Run lint**

Run from `src/Librory.Web`: `npm run lint`

Expected: no lint errors.

- [ ] **Step 3: Run the production build**

Run from `src/Librory.Web`: `npm run build`

Expected: TypeScript checks and Vite build pass.

- [ ] **Step 4: Inspect the final diff**

Run: `git diff origin/main...HEAD --check; git status --short`

Expected: only the approved design, plan, scan API transport, and scan-page changes are present; no generated artifacts are tracked.
