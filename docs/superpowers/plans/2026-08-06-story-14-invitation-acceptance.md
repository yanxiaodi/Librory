# Story 14 Invitation Acceptance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement the simple invitation acceptance flow while retaining a personal family for every account.

**Architecture:** Preserve the existing external login service and first-login bootstrap. Add only a validated local `returnUrl` through the OAuth start/callback pair, then let the invitation page call the already-existing preview and accept endpoints. After acceptance, select the invited family through the existing family selection endpoint.

**Tech Stack:** ASP.NET Core minimal APIs, cookie authentication, React 19, TypeScript, React Router, Vitest, Testing Library, xUnit.

## Global Constraints

- Every first login creates or reuses the account's personal family.
- Acceptance adds or binds a membership in the invited family; it never removes or merges families.
- Only local absolute-path return URLs are allowed.
- Invitation acceptance remains authenticated and email-bound.
- Reuse existing LoginPage/Card/Button and theme tokens.

---

### Task 1: Preserve a safe invitation return URL through external login

**Files:**
- Modify: `src/Librory.Api/Endpoints/AuthEndpoints.cs`
- Modify: `tests/Librory.Api.Tests/AuthEndpointsTests.cs`

- [ ] Add tests for provider start with a valid local return URL and an external URL.
- [ ] Update Google/Microsoft start handlers to carry only the validated local return URL into the callback redirect.
- [ ] Redirect successful callbacks to the validated URL, defaulting to `/app/home`.
- [ ] Run: `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --no-restore --filter FullyQualifiedName~AuthEndpointsTests -v minimal`
- [ ] Commit: `feat: preserve safe invitation return urls through login`

### Task 2: Add invitation preview and acceptance API client

**Files:**
- Modify: `src/Librory.Web/src/lib/familyApi.ts`
- Modify: `src/Librory.Web/src/lib/familyApi.test.ts`

- [ ] Add typed `InvitationPreview` and `getInvitationPreview(token)`.
- [ ] Add `acceptInvitation(token)` and use existing `selectFamily` after acceptance.
- [ ] Test URL encoding, credentials, POST method, and non-OK errors.
- [ ] Run: `npm run test:run -- src/lib/familyApi.test.ts`
- [ ] Commit: `feat: add invitation acceptance api client`

### Task 3: Add the invitation page and route

**Files:**
- Create: `src/Librory.Web/src/pages/InvitationPage.tsx`
- Modify: `src/Librory.Web/src/App.tsx`
- Modify: `src/Librory.Web/src/auth/authEndpoints.ts`
- Test: `src/Librory.Web/src/pages/InvitationPage.test.tsx`

- [ ] Test public preview, provider links containing the encoded local return URL, authenticated accept, email mismatch, and expired invitation states.
- [ ] Render the page outside `AuthGate`/`PublicOnlyGate` so both anonymous and authenticated users can access it.
- [ ] Use `useAuthSession` to choose sign-in or accept state; call `refreshSession` after selecting the invited family.
- [ ] Keep the page within the existing centered Card layout and use existing provider endpoints.
- [ ] Run: `npm run test:run -- src/pages/InvitationPage.test.tsx src/pages/App.test.tsx`
- [ ] Commit: `feat: add invitation preview and acceptance page`

### Task 4: Verify and update the Draft PR

**Files:**
- Create: `docs/devlog/2026-08-06-story-14-invitation-acceptance.md`

- [ ] Run backend focused tests and Web `npm run lint`, `npm run test:run`, and `npm run build`.
- [ ] Run `git diff --check` and inspect status.
- [ ] Record that browser review is attempted and report any environment limitation.
- [ ] Commit the devlog, push, and comment on PR #46 with the new flow and validation results.
