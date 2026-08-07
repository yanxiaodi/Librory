# Story 15 Recommendation Profile Web Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Settings-based, mobile-first UI for reading recommendation preferences for the current family member and, for administrators, other active family members.

**Architecture:** Keep HTTP contracts and profile mapping in `familyApi.ts`. Put form state, member selection, loading, permission, and save behavior in a focused `RecommendationProfileSection` component, then render that component from `SettingsPage`. Use the existing card, input, button, theme-token, and test patterns; do not add a new state library or connect this UI to scanning or AI workflows.

**Tech Stack:** React 19, TypeScript, React Testing Library, Vitest, Vite, Tailwind CSS v4, existing Librory Web API helpers.

## Global Constraints

- Keep the scope to Story 15 recommendation profile management; Story 16 scan context and Story 09 AI orchestration remain out of scope.
- Preserve the existing mobile-first Settings layout and CSS variable theme tokens.
- Treat HTTP 404 as an empty profile, HTTP 403 as read-only access, and other failures as retryable errors.
- Send empty collections as `[]` and cleared nullable values as `null` so the API's partial-update semantics are explicit.
- Use `PreferredLanguage` values `English = 0` and `Chinese = 1`.
- Do not expose private notes from unauthorized responses.

---

### Task 1: Add typed recommendation profile API contracts

**Files:**
- Modify: `src/Librory.Web/src/lib/familyApi.ts`
- Test: `src/Librory.Web/src/lib/familyApi.test.ts`

**Interfaces:**
- Produces `RecommendationProfile`, `RecommendationProfileUpdate`, `getMemberRecommendationProfile(memberId: string)`, and `updateMemberRecommendationProfile(memberId: string, input: RecommendationProfileUpdate)`.
- Preserves the existing `FamilyMember` shape while adding `hasRecommendationProfile`, `recommendationProfileVisibility`, and `canUseForFamilyRecommendations`.

- [ ] **Step 1: Write failing API helper tests**

Add tests that stub `fetch` and verify:

```ts
expect(fetchMock).toHaveBeenCalledWith(
  '/api/family/current/members/member-1/recommendation-profile',
  expect.objectContaining({ credentials: 'include' }),
)
```

Also verify the PUT helper sends `method: 'PUT'`, JSON content type, and a complete payload containing `null` scalar clears and empty collection arrays.

- [ ] **Step 2: Run the focused tests and confirm failure**

Run: `npm run test:run -- src/lib/familyApi.test.ts`

Expected: FAIL because the profile types and helpers do not exist.

- [ ] **Step 3: Add the profile types and helpers**

Model the API response as:

```ts
type RecommendationProfile = {
  memberId: string
  minimumAge: number | null
  maximumAge: number | null
  favoriteAuthors: string[]
  excludedAuthors: string[]
  favoriteGenres: string[]
  excludedGenres: string[]
  favoriteStyles: string[]
  excludedStyles: string[]
  preferredBookLanguages: Array<number | string>
  preferenceNotes: string | null
  profileVisibility: 'Family' | 'Private' | number
  useInFamilyRecommendations: boolean
}
```

Use `request<RecommendationProfile>` for the GET helper and `jsonRequest('PUT', input)` for the update helper. Add the three optional member-list metadata properties in camelCase.

- [ ] **Step 4: Run the focused tests and confirm success**

Run: `npm run test:run -- src/lib/familyApi.test.ts`

Expected: PASS with all family API tests.

- [ ] **Step 5: Commit the API contract slice**

```bash
git add src/Librory.Web/src/lib/familyApi.ts src/Librory.Web/src/lib/familyApi.test.ts
git commit -m "feat: add recommendation profile web api client"
```

### Task 2: Build the recommendation profile form component

**Files:**
- Create: `src/Librory.Web/src/components/family/RecommendationProfileSection.tsx`
- Create: `src/Librory.Web/src/components/family/RecommendationProfileSection.test.tsx`

**Interfaces:**
- Consumes `listMembers`, `getMemberRecommendationProfile`, and `updateMemberRecommendationProfile` from `familyApi.ts`.
- Accepts `{ isAdmin: boolean; currentMemberId?: string; refreshKey?: number }`.
- Produces a Settings card titled `Reading preferences` with accessible labels and save behavior.

- [ ] **Step 1: Write failing component tests for initial loading and populated fields**

Render the component with an admin session fixture, mock the member list and profile GET, and assert that the selected member's age, notes, language, and visibility controls show the API values.

- [ ] **Step 2: Run the focused component test and confirm failure**

Run: `npm run test:run -- src/components/family/RecommendationProfileSection.test.tsx`

Expected: FAIL because the component does not exist.

- [ ] **Step 3: Implement member selection and loading states**

Initialize the selected member from `currentMemberId`, fall back to the first active member, and fetch the profile whenever the selected member or `refreshKey` changes. Map 404 to a default empty form; map 403 to a read-only state; preserve form state when other errors occur. Render a loading message while fetching.

- [ ] **Step 4: Add form fields with stable local state**

Use controlled inputs for minimum/maximum age, notes, visibility, and family-recommendation participation. Use comma-separated text inputs for favorite/excluded authors, genres, and styles. Use English/Chinese checkboxes for preferred languages. Keep a local `ProfileFormState` with string values so partially typed input is not lost.

- [ ] **Step 5: Add explicit payload mapping**

Implement a local mapper with these exact rules:

```ts
minimumAge: form.minimumAge.trim() ? Number(form.minimumAge) : null
maximumAge: form.maximumAge.trim() ? Number(form.maximumAge) : null
favoriteAuthors: splitCommaList(form.favoriteAuthors)
excludedAuthors: splitCommaList(form.excludedAuthors)
favoriteGenres: splitCommaList(form.favoriteGenres)
excludedGenres: splitCommaList(form.excludedGenres)
favoriteStyles: splitCommaList(form.favoriteStyles)
excludedStyles: splitCommaList(form.excludedStyles)
preferenceNotes: form.preferenceNotes.trim() || null
```

Send `profileVisibility` and `useInFamilyRecommendations` as their current values. Disable save while saving and display a success/error status without clearing the form.

- [ ] **Step 6: Complete the component layout and permission behavior**

Render the member selector only when there are multiple selectable members. Show edit controls for the current member or an administrator. For a forbidden selected member, show `This profile is not available to you.` and no notes or edit controls. Keep the card scroll-friendly and use existing theme variables.

- [ ] **Step 7: Add focused interaction tests**

Cover:

Create four tests with these exact behaviors:

- Mock the profile GET as 404, enter a note, clear the age, save, and assert the PUT body contains `preferenceNotes: '...'`, `minimumAge: null`, and empty arrays for all collection fields.
- Render as an administrator with two active members, select the second member, and assert a second profile GET uses that member ID.
- Mock the profile GET as 403 and assert the permission message is visible and no save button or preference notes field is rendered.
- Mock the PUT as a 500, change a form value, save, and assert the error is visible while the changed value remains in its input.

- [ ] **Step 8: Run the component tests and confirm success**

Run: `npm run test:run -- src/components/family/RecommendationProfileSection.test.tsx`

Expected: PASS with all new interaction tests.

- [ ] **Step 9: Commit the component slice**

```bash
git add src/Librory.Web/src/components/family/RecommendationProfileSection.tsx src/Librory.Web/src/components/family/RecommendationProfileSection.test.tsx
git commit -m "feat: add recommendation profile settings form"
```

### Task 3: Integrate the component into Settings

**Files:**
- Modify: `src/Librory.Web/src/pages/SettingsPage.tsx`
- Modify: `src/Librory.Web/src/pages/SettingsPage.test.tsx`

**Interfaces:**
- Consumes the component props from Task 2 and the existing authenticated session role/member ID.
- Produces a visible `Reading preferences` section between family selection and member/invitation management.

- [ ] **Step 1: Add a failing Settings integration assertion**

Extend the authenticated Settings fixture so the mocked member list includes profile metadata, then assert the Settings page renders the `Reading preferences` heading and loads the profile endpoint.

- [ ] **Step 2: Run the Settings test and confirm failure**

Run: `npm run test:run -- src/pages/SettingsPage.test.tsx`

Expected: FAIL because Settings does not render the new section.

- [ ] **Step 3: Render the section with current session context**

Pass `isAdmin={session.user?.role === 'Admin'}`, `currentMemberId={session.family?.memberId}` once the session type exposes it, and `refreshKey={familyRefreshKey}`. Keep the existing family/member/invitation sections unchanged.

- [ ] **Step 4: Add the current member ID to the frontend session projection**

Modify `src/Librory.Web/src/auth/authSessionTypes.ts` so `FamilySummary` includes `memberId: string`. In `AuthSessionContext.tsx`, copy `CurrentFamilyResponse.memberId` into `family.memberId`. Update authenticated test fixtures in `AuthSessionContext.test.tsx`, `AuthGate.test.tsx`, `App.test.tsx`, and `SettingsPage.test.tsx` with the existing fixture member ID. Do not change backend contracts in this frontend PR.

- [ ] **Step 5: Run the Settings tests and confirm success**

Run: `npm run test:run -- src/pages/SettingsPage.test.tsx`

Expected: PASS with existing Settings behavior and the new preference section.

- [ ] **Step 6: Commit the Settings integration**

```bash
git add src/Librory.Web/src/pages/SettingsPage.tsx src/Librory.Web/src/pages/SettingsPage.test.tsx src/Librory.Web/src/auth/authSessionTypes.ts
git commit -m "feat: add recommendation preferences to settings"
```

### Task 4: Document and validate the frontend slice

**Files:**
- Create: `docs/devlog/2026-08-07-story-15-recommendation-profile-web.md`

- [ ] **Step 1: Add the devlog entry**

Record the Settings location, member-scoped loading behavior, explicit clear payload semantics, 403 privacy behavior, and the boundary with Story 16.

- [ ] **Step 2: Run the full frontend test suite**

Run: `npm run test:run`

Expected: PASS with zero failures.

- [ ] **Step 3: Run frontend lint**

Run: `npm run lint`

Expected: exit code 0 with no lint errors.

- [ ] **Step 4: Run frontend build**

Run: `npm run build`

Expected: exit code 0 with successful TypeScript checks and Vite output.

- [ ] **Step 5: Perform browser review**

Run the frontend dev server and inspect `/app/settings` at a narrow mobile viewport and a desktop viewport. Verify the long form scrolls inside the existing shell, labels and controls remain readable, and the Settings bottom navigation stays fixed. Record any observations in the devlog.

- [ ] **Step 6: Self-review the final diff**

Run:

```bash
git diff main...HEAD --stat
git diff main...HEAD --check
git status --short
```

Confirm only the frontend API, component, Settings integration, tests, spec, plan, and devlog files are present.

- [ ] **Step 7: Commit documentation and validation notes**

```bash
git add docs/devlog/2026-08-07-story-15-recommendation-profile-web.md
git commit -m "docs: record story 15 recommendation profile web"
```
