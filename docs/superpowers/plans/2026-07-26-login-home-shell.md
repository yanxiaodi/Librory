# Login and Home Shell Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn Librory into a login-gated app with a public landing page, a dedicated login page, and an authenticated home screen centered on shelf scanning.

**Architecture:** Keep the public and private surfaces separate. `/` and `/login` belong to the public entry flow, while `/app/*` holds the authenticated product shell and the existing bottom navigation. Use a small auth state layer so route guards can redirect anonymously to login and send authenticated users straight to `/app/home`, while still allowing the first-login singleton-family bootstrap path.

**Tech Stack:** React 19, TypeScript, React Router DOM, Vite, Tailwind CSS v4, lucide-react, existing shell/theme components, Vitest, React Testing Library.

## Global Constraints

- Public pages must not expose private app data.
- Unauthenticated users can only reach public pages and login.
- Authenticated users go directly to `/app/home`.
- A user with no family yet must still be able to use the app as a one-person family.
- The home screen must keep the scan action prominent.
- The app should remain usable when a family has only one member.

---

### Task 1: Add an auth session layer and route guards

**Files:**
- Modify: `src/Librory.Web/src/main.tsx`
- Modify: `src/Librory.Web/src/App.tsx`
- Create: `src/Librory.Web/src/auth/authSessionTypes.ts`
- Create: `src/Librory.Web/src/auth/AuthSessionContext.tsx`
- Create: `src/Librory.Web/src/auth/useAuthSession.ts`
- Create: `src/Librory.Web/src/auth/AuthGate.tsx`
- Create: `src/Librory.Web/src/auth/PublicOnlyGate.tsx`
- Create: `src/Librory.Web/src/auth/AuthGate.test.tsx`

**Interfaces:**
- `type AuthStatus = 'loading' | 'anonymous' | 'authenticated'`
- `type FamilySummary = { id: string; name: string; memberCount: number }`
- `type AuthUser = { id: string; displayName: string; email?: string | null }`
- `type AuthSession = { status: AuthStatus; user: AuthUser | null; family: FamilySummary | null }`
- `function AuthSessionProvider({ children, initialSession }: { children: React.ReactNode; initialSession?: AuthSession })`
- `function useAuthSession(): AuthSession`
- `function AuthGate({ children }: { children: React.ReactNode })`
- `function PublicOnlyGate({ children }: { children: React.ReactNode })`

- [ ] **Step 1: Write the redirect tests first**

```tsx
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import App from '@/App'
import { AuthSessionProvider } from '@/auth/AuthSessionContext'

render(
  <MemoryRouter initialEntries={['/app/home']}>
    <AuthSessionProvider initialSession={{ status: 'anonymous', user: null, family: null }}>
      <App />
    </AuthSessionProvider>
  </MemoryRouter>,
)

expect(screen.getByRole('heading', { name: /sign in/i })).toBeVisible()
```

- [ ] **Step 2: Run the test to confirm the guard does not exist yet**

Run: `npm run test:run -- src/auth/AuthGate.test.tsx`
Expected: fail because the auth session layer and route guards do not exist yet.

- [ ] **Step 3: Implement the session context and route guards**

```tsx
export function AuthGate({ children }: { children: React.ReactNode }) {
  const { status } = useAuthSession()

  if (status === 'loading') {
    return <div>Loading...</div>
  }

  if (status === 'anonymous') {
    return <Navigate to="/login" replace />
  }

  return <>{children}</>
}
```

```tsx
export function PublicOnlyGate({ children }: { children: React.ReactNode }) {
  const { status } = useAuthSession()

  if (status === 'authenticated') {
    return <Navigate to="/app/home" replace />
  }

  return children
}
```

- [ ] **Step 4: Run the tests again and verify redirects work**

Run: `npm run test:run -- src/auth/AuthGate.test.tsx`
Expected: pass, with anonymous users redirected to `/login` and authenticated users redirected away from public pages.

- [ ] **Step 5: Commit the auth boundary**

```bash
git add src/Librory.Web/src/main.tsx src/Librory.Web/src/App.tsx src/Librory.Web/src/auth
git commit -m "feat: add auth route guards"
```

### Task 2: Build the public landing page and login page

**Files:**
- Create: `src/Librory.Web/src/pages/LandingPage.tsx`
- Create: `src/Librory.Web/src/pages/LoginPage.tsx`
- Create: `src/Librory.Web/src/pages/LandingPage.test.tsx`
- Create: `src/Librory.Web/src/pages/LoginPage.test.tsx`
- Modify: `src/Librory.Web/src/App.tsx`

**Interfaces:**
- `function LandingPage(): JSX.Element`
- `function LoginPage(): JSX.Element`

- [ ] **Step 1: Write page-level tests for the public entry flow**

```tsx
render(
  <MemoryRouter initialEntries={['/']}>
    <App />
  </MemoryRouter>,
)

expect(screen.getByRole('heading', { name: /librory/i })).toBeVisible()
expect(screen.getByRole('link', { name: /sign in/i })).toBeVisible()
```

```tsx
render(
  <MemoryRouter initialEntries={['/login']}>
    <App />
  </MemoryRouter>,
)

expect(screen.getByRole('heading', { name: /sign in/i })).toBeVisible()
expect(screen.getByRole('button', { name: /google/i })).toBeVisible()
expect(screen.getByRole('button', { name: /microsoft/i })).toBeVisible()
expect(screen.getByRole('button', { name: /email/i })).toBeVisible()
```

- [ ] **Step 2: Run the tests to confirm the public pages are not implemented yet**

Run: `npm run test:run -- src/pages/LandingPage.test.tsx src/pages/LoginPage.test.tsx`
Expected: fail because the public landing and login pages do not exist yet.

- [ ] **Step 3: Implement the public pages and connect them to the route tree**

```tsx
export function LandingPage() {
  return (
    <main>
      <h1>Librory</h1>
      <p>Scan bookshop shelves fast, then decide what is worth buying.</p>
      <a href="/login">Sign in</a>
    </main>
  )
}
```

```tsx
export function LoginPage() {
  return (
    <main>
      <h1>Sign in</h1>
      <button type="button">Continue with Google</button>
      <button type="button">Continue with Microsoft</button>
      <button type="button">Continue with email</button>
    </main>
  )
}
```

- [ ] **Step 4: Run the page tests again and verify the UI renders**

Run: `npm run test:run -- src/pages/LandingPage.test.tsx src/pages/LoginPage.test.tsx`
Expected: pass, with the landing page staying public and the login page presenting all three sign-in choices.

- [ ] **Step 5: Commit the public entry pages**

```bash
git add src/Librory.Web/src/pages/LandingPage.tsx src/Librory.Web/src/pages/LoginPage.tsx src/Librory.Web/src/App.tsx
git commit -m "feat: add public landing and login pages"
```

### Task 3: Move the existing shell into `/app/*` and build the authenticated home page

**Files:**
- Modify: `src/Librory.Web/src/App.tsx`
- Modify: `src/Librory.Web/src/pages/HomePage.tsx`
- Modify: `src/Librory.Web/src/pages/ScansPage.tsx`
- Modify: `src/Librory.Web/src/pages/LibraryPage.tsx`
- Modify: `src/Librory.Web/src/pages/SettingsPage.tsx`
- Create: `src/Librory.Web/src/pages/HomePage.test.tsx`
- Create: `src/Librory.Web/src/components/home/HomeSummaryStrip.tsx`
- Create: `src/Librory.Web/src/components/home/PrimaryScanAction.tsx`

**Interfaces:**
- `type HomeSummary = { bookCount: number; scanCount: number; familySize: number }`
- `function HomeSummaryStrip({ summary }: { summary: HomeSummary })`
- `function PrimaryScanAction(): JSX.Element`
- `function HomePage(): JSX.Element`

- [ ] **Step 1: Write the home-page test around the scan-first prototype**

```tsx
render(
  <MemoryRouter initialEntries={['/app/home']}>
    <AuthSessionProvider
      initialSession={{
        status: 'authenticated',
        user: { id: 'user-1', displayName: 'Alice', email: 'alice@example.com' },
        family: { id: 'family-1', name: 'The Yans', memberCount: 1 },
      }}
    >
      <App />
    </AuthSessionProvider>
  </MemoryRouter>,
)

expect(screen.getByRole('button', { name: /scan a shelf/i })).toBeVisible()
expect(screen.getByText(/books saved/i)).toBeVisible()
expect(screen.getByText('1')).toBeVisible()
```

- [ ] **Step 2: Run the test and confirm the home page still shows the old placeholder**

Run: `npm run test:run -- src/pages/HomePage.test.tsx`
Expected: fail until the scan-first home layout replaces the placeholder.

- [ ] **Step 3: Implement the authenticated home page and reuse the existing shell**

```tsx
export function HomePage() {
  return (
    <main>
      <PrimaryScanAction />
      <HomeSummaryStrip summary={{ bookCount: 0, scanCount: 0, familySize: 1 }} />
    </main>
  )
}
```

The shell should continue to render the existing bottom navigation, but the route map should move from `/`, `/scans`, `/library`, and `/settings` to `/app/home`, `/app/scans`, `/app/library`, and `/app/settings`.

- [ ] **Step 4: Run the home-page test again and verify the scan CTA is dominant**

Run: `npm run test:run -- src/pages/HomePage.test.tsx`
Expected: pass, with the home screen feeling like a quick launch pad for shelf scanning rather than a general dashboard.

- [ ] **Step 5: Commit the authenticated shell**

```bash
git add src/Librory.Web/src/pages/HomePage.tsx src/Librory.Web/src/components/home src/Librory.Web/src/App.tsx
git commit -m "feat: add authenticated home shell"
```

### Task 4: Update navigation tests, run the full web verification, and document the new flow

**Files:**
- Modify: `src/Librory.Web/src/App.test.tsx`
- Modify: `src/Librory.Web/src/pages/SettingsPage.test.tsx`
- Modify: `docs/frontend-integration-guide.md`
- Modify: `docs/story-map-mvp.md` only if the route names or auth flow need a follow-up wording tweak

**Interfaces:**
- `App` must now render both public and authenticated route groups
- The auth test helpers from Task 1 must be reusable by the UI route tests

- [ ] **Step 1: Update the shell tests to cover the new public and private routes**

```tsx
render(
  <MemoryRouter initialEntries={['/']}>
    <App />
  </MemoryRouter>,
)

expect(screen.getByRole('heading', { name: /librory/i })).toBeVisible()
expect(screen.getByRole('link', { name: /sign in/i })).toBeVisible()
```

```tsx
render(
  <MemoryRouter initialEntries={['/app/settings']}>
    <AuthSessionProvider
      initialSession={{
        status: 'authenticated',
        user: { id: 'user-1', displayName: 'Alice', email: 'alice@example.com' },
        family: { id: 'family-1', name: 'The Yans', memberCount: 1 },
      }}
    >
      <App />
    </AuthSessionProvider>
  </MemoryRouter>,
)

expect(screen.getByRole('heading', { name: /settings/i })).toBeVisible()
expect(screen.getByRole('link', { name: /home/i })).toBeVisible()
```

- [ ] **Step 2: Run the focused web tests**

Run:
`npm run test:run -- src/auth/AuthGate.test.tsx src/pages/LandingPage.test.tsx src/pages/LoginPage.test.tsx src/pages/HomePage.test.tsx src/App.test.tsx`

Expected:
- public routes render
- unauthenticated app routes redirect to login
- authenticated routes stay inside `/app/*`
- the home page keeps the scan-first structure

- [ ] **Step 3: Run the web build**

Run: `npm run build`
Expected: pass without route or import errors.

- [ ] **Step 4: Update the integration guide**

Document the new front-door sequence:

```md
1. Visit `/` for the public landing page.
2. Sign in at `/login`.
3. Land on `/app/home`.
4. Use the scan button as the default next action.
```

- [ ] **Step 5: Commit the verification and docs**

```bash
git add src/Librory.Web/src/App.test.tsx src/Librory.Web/src/pages/SettingsPage.test.tsx docs/frontend-integration-guide.md
git commit -m "test: cover login and home shell flow"
```

## Coverage Check

The plan covers every requirement from `docs/superpowers/specs/2026-07-26-login-home-shell-design.md`:

- public landing page and screenshots: Task 2
- login page with Google, Microsoft, and email entry: Task 2
- authenticated `/app/*` shell: Task 1 and Task 3
- default post-login route `/app/home`: Task 1 and Task 3
- unauthenticated redirect to `/login`: Task 1 and Task 4
- authenticated redirect away from public pages: Task 1 and Task 4
- singleton-family support for solo users: Task 1 and Task 3
- scan-first home layout: Task 3
- test coverage and build verification: Task 4

## Notes

- Keep the first pass narrow: route separation, public entry pages, a scan-first home page, and tests.
- Do not wire full Google, Microsoft, or email provider flows until the backend/auth story exposes the real endpoints.
- Preserve the existing theme system and bottom navigation pattern; only move it under `/app/*`.
