# Frontend Auth Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Connect the Librory web app to the real backend Google and Microsoft login flow so users can sign in, land in `/app/home`, and use the app immediately without any activation step.

**Architecture:** Keep the frontend thin and let the backend own OAuth. The login page should only navigate to the backend auth start routes, the auth session context should keep hydrating from `/api/family/current`, and logout should call the backend logout route before returning the user to the public landing page. Leave the existing public/private route split intact.

**Tech Stack:** React 19, TypeScript, React Router DOM, Vite, Tailwind CSS v4, lucide-react, existing auth/session and shell components, Vitest, React Testing Library.

## Global Constraints

- The login page should present only Google and Microsoft.
- Email login or registration stays out of scope for this slice.
- Any activation or onboarding step after login stays out of scope.
- `/` stays public.
- `/login` stays public.
- `/app/*` stays authenticated.
- Successful login should land the user on `/app/home`.
- Logout should return the user to the public landing page.
- Local dev auth stays available for debugging if needed.

---

### Task 1: Add shared auth URLs and switch session logout to the real backend endpoint

**Files:**
- Create: `src/Librory.Web/src/auth/authEndpoints.ts`
- Modify: `src/Librory.Web/src/auth/AuthSessionContext.tsx`
- Modify: `src/Librory.Web/src/auth/AuthSessionContext.test.tsx`

**Interfaces:**
- `authEndpoints.googleStart`
- `authEndpoints.microsoftStart`
- `authEndpoints.logout`
- `signOut(): Promise<void>` should call `POST /auth/logout`
- `signInWithDevLogin(...)` remains available for local debug tests, but it is no longer used by the public login page

- [ ] **Step 1: Write the failing logout test first**

```tsx
it('signs out through the backend logout endpoint', async () => {
  const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
    expect(String(input)).toBe('/auth/logout')

    return new Response('', { status: 204 })
  })

  vi.stubGlobal('fetch', fetchMock)

  const user = userEvent.setup()
  renderProbe({
    status: 'authenticated',
    user: { id: 'member-1', displayName: 'Alice', email: 'alice@example.com' },
    family: { id: 'family-1', name: 'The Yans', memberCount: 2 },
  })

  await user.click(screen.getByRole('button', { name: /sign out/i }))

  expect(fetchMock).toHaveBeenCalledWith(
    '/auth/logout',
    expect.objectContaining({
      method: 'POST',
      credentials: 'include',
    }),
  )
  expect(await screen.findByText('anonymous')).toBeVisible()
})
```

- [ ] **Step 2: Run the auth session test to confirm it fails on the old dev logout path**

Run: `npm run test:run -- src/auth/AuthSessionContext.test.tsx`
Expected: fail because `signOut` still points at `/dev/auth/logout`.

- [ ] **Step 3: Implement the shared auth URLs and switch `signOut`**

```tsx
export const authEndpoints = {
  googleStart: '/auth/google/start',
  microsoftStart: '/auth/microsoft/start',
  logout: '/auth/logout',
} as const
```

```tsx
const signOut = useCallback(async () => {
  await fetch(authEndpoints.logout, {
    method: 'POST',
    credentials: 'include',
  })

  setSession(anonymousSession)
}, [])
```

- [ ] **Step 4: Run the auth session test again and verify it passes**

Run: `npm run test:run -- src/auth/AuthSessionContext.test.tsx`
Expected: pass, with the dev login helper still working for local debug tests and logout now calling the backend route.

- [ ] **Step 5: Commit the auth endpoint wiring**

```bash
git add src/Librory.Web/src/auth/authEndpoints.ts src/Librory.Web/src/auth/AuthSessionContext.tsx src/Librory.Web/src/auth/AuthSessionContext.test.tsx
git commit -m "feat: wire frontend auth endpoints"
```

### Task 2: Replace the login page with real Google and Microsoft links

**Files:**
- Modify: `src/Librory.Web/src/pages/LoginPage.tsx`
- Modify: `src/Librory.Web/src/pages/LoginPage.test.tsx`

**Interfaces:**
- Login page should render two provider links only:
  - Google -> `authEndpoints.googleStart`
  - Microsoft -> `authEndpoints.microsoftStart`
- The page should no longer call `signInWithDevLogin`
- The email button should not be rendered in this slice

- [ ] **Step 1: Write the failing login-page test around the real auth links**

```tsx
render(
  <MemoryRouter>
    <AuthSessionProvider initialSession={{ status: 'anonymous', user: null, family: null }}>
      <LoginPage />
    </AuthSessionProvider>
  </MemoryRouter>,
)

expect(screen.getByRole('link', { name: /continue with google/i })).toHaveAttribute(
  'href',
  '/auth/google/start',
)
expect(screen.getByRole('link', { name: /continue with microsoft/i })).toHaveAttribute(
  'href',
  '/auth/microsoft/start',
)
expect(screen.queryByRole('button', { name: /continue with email/i })).not.toBeInTheDocument()
```

- [ ] **Step 2: Run the login-page test and confirm it fails on the old dev-login UI**

Run: `npm run test:run -- src/pages/LoginPage.test.tsx`
Expected: fail because the current page still renders dev-login buttons and an email button.

- [ ] **Step 3: Replace the dev-login buttons with real auth links**

```tsx
<Button asChild size="lg" variant="outline">
  <a href={authEndpoints.googleStart}>Continue with Google</a>
</Button>

<Button asChild size="lg" variant="outline">
  <a href={authEndpoints.microsoftStart}>Continue with Microsoft</a>
</Button>
```

Remove the provider state machine and the `useAuthSessionActions().signInWithDevLogin` call from the page.

- [ ] **Step 4: Run the login-page test again and verify it passes**

Run: `npm run test:run -- src/pages/LoginPage.test.tsx`
Expected: pass, with the login page exposing only the two real provider entry points.

- [ ] **Step 5: Commit the login-page swap**

```bash
git add src/Librory.Web/src/pages/LoginPage.tsx src/Librory.Web/src/pages/LoginPage.test.tsx
git commit -m "feat: wire login page to backend auth"
```

### Task 3: Add a real logout action to settings and update the frontend integration guide

**Files:**
- Modify: `src/Librory.Web/src/pages/SettingsPage.tsx`
- Modify: `src/Librory.Web/src/pages/SettingsPage.test.tsx`
- Modify: `docs/frontend-integration-guide.md`
- Add: `docs/devlog/2026-07-26-frontend-auth-integration.md`

**Interfaces:**
- `SettingsPage` should expose a visible sign-out action
- Clicking sign-out should call the backend logout endpoint through `useAuthSessionActions().signOut`
- After logout, the page should navigate the user to `/`

- [ ] **Step 1: Write the failing logout-flow test on the settings page**

```tsx
it('logs out and returns to the landing page', async () => {
  const user = userEvent.setup()
  const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
    expect(String(input)).toBe('/auth/logout')
    return new Response('', { status: 204 })
  })

  vi.stubGlobal('fetch', fetchMock)

  render(
    <MemoryRouter initialEntries={['/app/settings']}>
      <ThemeRoot>
        <AuthSessionProvider
          initialSession={{
            status: 'authenticated',
            user: { id: 'user-1', displayName: 'Alice', email: 'alice@example.com' },
            family: { id: 'family-1', name: 'The Yans', memberCount: 1 },
          }}
        >
          <App />
        </AuthSessionProvider>
      </ThemeRoot>
    </MemoryRouter>,
  )

  await user.click(screen.getByRole('button', { name: /sign out/i }))

  expect(await screen.findByRole('link', { name: /sign in/i })).toBeVisible()
})
```

- [ ] **Step 2: Run the settings-page test and confirm it fails because logout does not exist yet**

Run: `npm run test:run -- src/pages/SettingsPage.test.tsx`
Expected: fail because the settings page still only renders theme controls and a placeholder.

- [ ] **Step 3: Implement the sign-out action and landing-page redirect**

```tsx
const { signOut } = useAuthSessionActions()
const navigate = useNavigate()

const handleSignOut = async () => {
  await signOut()
  navigate('/')
}
```

Render the action in the settings page as a clear secondary control near the existing theme section.

- [ ] **Step 4: Update the frontend integration guide and devlog**

Document the new flow in `docs/frontend-integration-guide.md`:

```md
1. Visit `/` for the public landing page.
2. Sign in at `/login`.
3. Click Google or Microsoft.
4. Land on `/app/home`.
5. Use sign out from settings to return to `/`.
```

Add a short devlog note in `docs/devlog/2026-07-26-frontend-auth-integration.md` describing the real backend auth integration and the removal of the activation step.

- [ ] **Step 5: Run the focused web verification**

Run:
`npm run test:run -- src/auth/AuthSessionContext.test.tsx src/pages/LoginPage.test.tsx src/pages/SettingsPage.test.tsx src/auth/AuthGate.test.tsx src/App.test.tsx`

Then run:
`npm run build`

Expected:
- login buttons go to the backend auth start routes
- logout clears the session and returns to `/`
- auth guards still protect `/app/*`
- the web app builds cleanly

- [ ] **Step 6: Commit the frontend integration docs and logout flow**

```bash
git add src/Librory.Web/src/pages/SettingsPage.tsx src/Librory.Web/src/pages/SettingsPage.test.tsx docs/frontend-integration-guide.md docs/devlog/2026-07-26-frontend-auth-integration.md
git commit -m "feat: add frontend auth integration"
```

## Coverage Check

The plan covers every requirement from `docs/superpowers/specs/2026-07-26-frontend-auth-integration-design.md`:

- real Google sign-in links: Task 2
- real Microsoft sign-in links: Task 2
- no email login in this slice: Task 2
- no activation step: Tasks 1 and 2
- `/api/family/current` hydration remains the source of truth: Task 1
- logout returns to the public landing page: Task 3
- public/private routes remain intact: Tasks 2 and 3
- local dev auth stays available for debugging: Task 1
- frontend integration docs and devlog update: Task 3

## Notes

- Keep the frontend thin and avoid re-implementing OAuth in React.
- Do not add the email provider button back until the backend actually exposes an email auth flow.
- Prefer simple link navigation for provider starts so the browser can follow the backend auth round trip cleanly.
