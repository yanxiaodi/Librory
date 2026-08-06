# Story 14 Family Management Web Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a Settings-based family management experience that matches the existing Librory Web visual language and consumes the Story 14 family/member/invitation APIs.

**Architecture:** Keep `SettingsPage` as the route and compose three focused sections for family selection, members, and invitations. Put all HTTP details in a typed `familyApi.ts` module; page state owns loading, mutation, and inline error feedback. Reuse existing `PageFrame`, `Button`, theme tokens, and mobile-first spacing.

**Tech Stack:** React 19, TypeScript, Vite, Tailwind CSS v4, Vitest, Testing Library, existing Librory Web components.

## Global Constraints

- Keep the existing Settings route and bottom navigation unchanged.
- Use `credentials: 'include'` for every family API request.
- Do not add a state-management dependency or new color tokens.
- Keep one-time invitation URLs only in create/resend mutation state; never display them in the invitation list.
- Preserve existing Settings theme and logout behavior.

---

### Task 1: Add the typed family API client

**Files:**
- Create: `src/Librory.Web/src/lib/familyApi.ts`
- Test: `src/Librory.Web/src/lib/familyApi.test.ts`

**Interfaces:**
- `listFamilies(): Promise<FamilySummary[]>`
- `selectFamily(familyId: string): Promise<FamilySummary>`
- `listMembers(): Promise<FamilyMember[]>`
- `createMember(input: CreateMemberInput): Promise<FamilyMember>`
- `updateMember(memberId: string, input: UpdateMemberInput): Promise<FamilyMember>`
- `setMemberActive(memberId: string, active: boolean): Promise<FamilyMember>`
- `listInvitations(): Promise<FamilyInvitation[]>`
- `createInvitation(input: CreateInvitationInput): Promise<FamilyInvitation>`
- `resendInvitation(invitationId: string): Promise<FamilyInvitation>`
- `revokeInvitation(invitationId: string): Promise<FamilyInvitation>`

- [ ] **Step 1: Write failing fetch contract tests**

Cover one read, one JSON mutation, the active-state endpoint, and a non-OK response. Assert URL, method, `credentials: 'include'`, JSON headers/body, and parsed result.

- [ ] **Step 2: Run the focused test and verify it fails**

Run: `npm run test:run -- src/lib/familyApi.test.ts`

Expected: FAIL because `familyApi.ts` does not exist.

- [ ] **Step 3: Implement typed contracts and request helper**

Define response types matching the API JSON casing, centralize `fetch`, parse JSON when present, and throw `FamilyApiError` with status and server message for non-2xx responses.

- [ ] **Step 4: Run the focused test and verify it passes**

Run: `npm run test:run -- src/lib/familyApi.test.ts`

Expected: all API client tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Librory.Web/src/lib/familyApi.ts src/Librory.Web/src/lib/familyApi.test.ts
git commit -m "feat: add family management web api client"
```

### Task 2: Build family selection and member management sections

**Files:**
- Create: `src/Librory.Web/src/components/family/FamilySection.tsx`
- Create: `src/Librory.Web/src/components/family/MembersSection.tsx`
- Modify: `src/Librory.Web/src/pages/SettingsPage.tsx`
- Test: `src/Librory.Web/src/pages/SettingsPage.test.tsx`

**Interfaces:**
- `FamilySection` receives `onFamilySelected: () => Promise<void>` and calls `listFamilies`/`selectFamily`.
- `MembersSection` receives no API props and calls the client functions internally; it renders admin actions based on the current session member role.

- [ ] **Step 1: Extend test fixtures with family/member API responses**

Add authenticated admin session fixtures and a fetch router for `/api/families`, `/api/family/current/members`, and member mutation endpoints. Add tests for family selection and placeholder-member creation.

- [ ] **Step 2: Run the Settings tests and verify the new tests fail**

Run: `npm run test:run -- src/pages/SettingsPage.test.tsx`

Expected: FAIL because the new controls are not rendered.

- [ ] **Step 3: Implement `FamilySection`**

Render a stacked card with the current family name, member count, and a labeled `<select>`. On change, call `selectFamily`, then `refreshSession`; show disabled/loading state and an inline error without leaving Settings.

- [ ] **Step 4: Implement `MembersSection`**

Render member rows with display name, role, active/deactivated status, and linked-account status. For admins, add a compact form for display name and preferred language, plus edit, deactivate, and reactivate controls. Use existing button variants and token classes.

- [ ] **Step 5: Compose sections in `SettingsPage`**

Keep ThemeSelect and logout unchanged. Replace the placeholder language/family card with the new family and members sections, preserving the existing PageFrame copy and single-column spacing.

- [ ] **Step 6: Run the Settings tests and verify they pass**

Run: `npm run test:run -- src/pages/SettingsPage.test.tsx`

Expected: existing theme/logout tests and new family/member tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/Librory.Web/src/components/family/FamilySection.tsx src/Librory.Web/src/components/family/MembersSection.tsx src/Librory.Web/src/pages/SettingsPage.tsx src/Librory.Web/src/pages/SettingsPage.test.tsx
git commit -m "feat: add family and member settings sections"
```

### Task 3: Build invitation management section

**Files:**
- Create: `src/Librory.Web/src/components/family/InvitationsSection.tsx`
- Modify: `src/Librory.Web/src/pages/SettingsPage.tsx`
- Test: `src/Librory.Web/src/pages/SettingsPage.test.tsx`

**Interfaces:**
- `InvitationsSection` loads `listInvitations` and supports `createInvitation`, `resendInvitation`, and `revokeInvitation`.

- [ ] **Step 1: Add failing invitation interaction tests**

Test creating an email invitation, rendering the returned one-time URL with a copy button, and ensuring the normal invitation list does not render a token. Test resend and revoke calls use the correct invitation id.

- [ ] **Step 2: Run the focused Settings tests and verify they fail**

Run: `npm run test:run -- src/pages/SettingsPage.test.tsx`

Expected: FAIL because the invitation section and controls are not present.

- [ ] **Step 3: Implement `InvitationsSection`**

Render a compact email form, status rows, and admin-only action buttons. Display the one-time URL only from the create/resend response, with `navigator.clipboard.writeText` and a clear success label. Clear the URL after a fresh list reload.

- [ ] **Step 4: Compose the invitation section in Settings**

Place invitations after members with the same section/card spacing. Keep the panel usable at the existing 430–460px mobile widths and allow long URLs to wrap without horizontal overflow.

- [ ] **Step 5: Run the focused tests and verify they pass**

Run: `npm run test:run -- src/pages/SettingsPage.test.tsx`

Expected: all Settings tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/Librory.Web/src/components/family/InvitationsSection.tsx src/Librory.Web/src/pages/SettingsPage.tsx src/Librory.Web/src/pages/SettingsPage.test.tsx
git commit -m "feat: add family invitation management UI"
```

### Task 4: Verify the Web slice and update the Draft PR

**Files:**
- Modify: `docs/devlog/2026-08-06-story-14-family-management-web.md`

- [ ] **Step 1: Run lint, tests, and production build**

Run from `src/Librory.Web`:

```bash
npm run lint
npm run test:run
npm run build
```

Expected: all commands exit 0.

- [ ] **Step 2: Review the responsive layout**

Run the existing Vite app and inspect Settings at the existing mobile width and a desktop width. Check that cards do not overflow, long invitation URLs wrap, disabled states are visible, and theme tokens remain consistent.

- [ ] **Step 3: Add a concise devlog entry**

Record the API boundary, Settings composition, one-time URL handling, and validation commands. Do not claim invitation registration or acceptance UI is complete.

- [ ] **Step 4: Commit the devlog**

```bash
git add docs/devlog/2026-08-06-story-14-family-management-web.md
git commit -m "docs: record story 14 family management web slice"
```

- [ ] **Step 5: Push and update PR #46**

```bash
git push
gh pr comment 46 --body "Added the first Story 14 web slice: family switching, member management, and invitation management in Settings. Frontend lint, tests, build, and responsive review completed. Invitation acceptance/onboarding remains deferred."
```
