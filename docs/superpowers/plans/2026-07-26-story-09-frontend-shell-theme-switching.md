# Frontend Shell and Theme Switching Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first real Librory web shell with four switchable pages, a settings page for style selection, and a persisted theme system that defaults to Botanical Press.

**Architecture:** Keep the app as one mobile-first shell with routed page placeholders and a bottom navigation bar. The layout should stay desktop-compatible, but it does not need a separate desktop information architecture yet. Put theme state at the root so every page inherits the same tokens, and persist the current theme in local storage until the backend member-preference API is ready. The settings page should stay small: one select control for style, plus placeholder sections for future language and account settings.

**Tech Stack:** React 19, TypeScript, React Router DOM, Vite, Tailwind CSS v4, existing `button` and `card` primitives, localStorage, Vitest + React Testing Library for UI coverage.

---

### Task 1: Replace the landing page with a real app shell and four routes

**Files:**
- Modify: `src/Librory.Web/src/App.tsx`
- Modify: `src/Librory.Web/src/main.tsx`
- Create: `src/Librory.Web/src/pages/HomePage.tsx`
- Create: `src/Librory.Web/src/pages/ScansPage.tsx`
- Create: `src/Librory.Web/src/pages/LibraryPage.tsx`
- Create: `src/Librory.Web/src/pages/SettingsPage.tsx`

- [ ] **Step 1: Implement the shell and page placeholders**

```tsx
import { NavLink, Route, Routes } from 'react-router-dom'

const navItems = [
  { to: '/', label: 'Home' },
  { to: '/scans', label: 'Scans' },
  { to: '/library', label: 'Library' },
  { to: '/settings', label: 'Settings' },
]

export default function App() {
  return (
    <div className="app-shell">
      <main className="app-shell__content">
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/scans" element={<ScansPage />} />
          <Route path="/library" element={<LibraryPage />} />
          <Route path="/settings" element={<SettingsPage />} />
        </Routes>
      </main>
      <nav className="app-shell__nav" aria-label="Primary">
        {navItems.map((item) => (
          <NavLink key={item.to} to={item.to}>
            {item.label}
          </NavLink>
        ))}
      </nav>
    </div>
  )
}
```

- [ ] **Step 2: Commit the shell milestone**

```bash
git add src/Librory.Web/src/App.tsx src/Librory.Web/src/main.tsx src/Librory.Web/src/pages
git commit -m "feat: add app shell and bottom navigation"
```

### Task 2: Add test infrastructure and UI coverage for the shell

**Files:**
- Modify: `src/Librory.Web/package.json`
- Create: `src/Librory.Web/vitest.config.ts`
- Create: `src/Librory.Web/src/test/setup.ts`
- Create: `src/Librory.Web/src/App.test.tsx`

- [ ] **Step 1: Add the test runner dependencies and scripts**

```json
{
  "scripts": {
    "test": "vitest",
    "test:run": "vitest run"
  }
}
```

- [ ] **Step 2: Write the failing shell test**

```tsx
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import App from './App'

render(
  <MemoryRouter initialEntries={['/settings']}>
    <App />
  </MemoryRouter>,
)

expect(screen.getByRole('heading', { name: /settings/i })).toBeVisible()
expect(screen.getByRole('link', { name: /home/i })).toBeVisible()
```

- [ ] **Step 3: Run the shell test and confirm it fails before the implementation is complete**

Run: `npm run test:run -- src/App.test.tsx`
Expected: fail because the route-aware shell and placeholder pages are not present yet.

- [ ] **Step 4: Run the shell test again after Task 1 and verify the app shell renders**

Run: `npm run test:run -- src/App.test.tsx`
Expected: pass, with each route showing its own page title and the bottom nav always visible.

- [ ] **Step 5: Commit the test harness and coverage**

```bash
git add src/Librory.Web/package.json src/Librory.Web/vitest.config.ts src/Librory.Web/src/test src/Librory.Web/src/App.test.tsx
git commit -m "test: add shell coverage"
```

### Task 3: Add root-level theme state, theme tokens, and local storage persistence

**Files:**
- Create: `src/Librory.Web/src/theme/themeTypes.ts`
- Create: `src/Librory.Web/src/theme/themeRegistry.ts`
- Create: `src/Librory.Web/src/theme/useTheme.ts`
- Create: `src/Librory.Web/src/theme/ThemeRoot.tsx`
- Create: `src/Librory.Web/src/theme/theme.test.tsx`
- Modify: `src/Librory.Web/src/index.css`

- [ ] **Step 1: Write the failing persistence test**

```tsx
render(
  <MemoryRouter initialEntries={['/']}>
    <ThemeRoot />
  </MemoryRouter>,
)

expect(localStorage.getItem('librory.theme')).toBe('botanical-press')
```

- [ ] **Step 2: Run the test to confirm there is no theme state yet**

Run: `npm run test:run -- src/theme/theme.test.tsx`
Expected: fail because the theme registry and persistence hook do not exist yet.

- [ ] **Step 3: Implement the shared theme registry and root wrapper**

```ts
export const themeRegistry = {
  'classic-scholar': { label: 'Classic Scholar' },
  'modern-scout': { label: 'Modern Scout' },
  'cozy-archive': { label: 'Cozy Archive' },
  'botanical-press': { label: 'Botanical Press' },
} as const
```

```tsx
export function ThemeRoot({ children }: { children: React.ReactNode }) {
  const [theme, setTheme] = useTheme()
  return <div data-theme={theme}>{children}</div>
}
```

- [ ] **Step 4: Run the persistence test again and verify the default theme is written**

Run: `npm run test:run -- src/theme/theme.test.tsx`
Expected: pass, with Botanical Press loaded when no saved value exists and invalid values falling back to the same default.

- [ ] **Step 5: Commit the theme foundation**

```bash
git add src/Librory.Web/src/theme src/Librory.Web/src/index.css
git commit -m "feat: add theme registry and persistence"
```

### Task 4: Build the Settings page style dropdown and connect it to theme switching

**Files:**
- Modify: `src/Librory.Web/src/pages/SettingsPage.tsx`
- Modify: `src/Librory.Web/src/App.tsx`
- Create: `src/Librory.Web/src/components/theme/ThemeSelect.tsx`
- Create: `src/Librory.Web/src/pages/SettingsPage.test.tsx`

- [ ] **Step 1: Write the failing Settings page interaction test**

```tsx
render(<SettingsPage />)

await user.selectOptions(screen.getByLabelText(/style/i), 'cozy-archive')
expect(screen.getByLabelText(/style/i)).toHaveValue('cozy-archive')
```

- [ ] **Step 2: Run the test to verify the select does not exist yet**

Run: `npm run test:run -- src/pages/SettingsPage.test.tsx`
Expected: fail because the page still has no style control.

- [ ] **Step 3: Implement the dropdown and immediate theme update**

```tsx
<label htmlFor="style">Style</label>
<select id="style" value={theme} onChange={(event) => setTheme(event.target.value)}>
  <option value="classic-scholar">Classic Scholar</option>
  <option value="modern-scout">Modern Scout</option>
  <option value="cozy-archive">Cozy Archive</option>
  <option value="botanical-press">Botanical Press</option>
</select>
```

The dropdown should update the root theme instantly and keep the selected value in sync when the page is reopened.

- [ ] **Step 4: Run the interaction test again and verify the style changes immediately**

Run: `npm run test:run -- src/pages/SettingsPage.test.tsx`
Expected: pass, with the theme setter updating the selected option and root theme state in one action.

- [ ] **Step 5: Commit the settings slice**

```bash
git add src/Librory.Web/src/pages/SettingsPage.tsx src/Librory.Web/src/components/theme/ThemeSelect.tsx src/Librory.Web/src/App.tsx
git commit -m "feat: add settings theme selector"
```

### Task 5: Add build checks and verify the shell visually

**Files:**
- Modify: `src/Librory.Web/package.json`
- Review: `src/Librory.Web/src/App.tsx`
- Review: `src/Librory.Web/src/pages/SettingsPage.tsx`
- Review: `src/Librory.Web/src/theme/themeRegistry.ts`

- [ ] **Step 1: Add coverage for the default theme, route switching, and persistence**

```tsx
expect(screen.getByRole('link', { name: /scans/i })).toBeVisible()
expect(screen.getByRole('heading', { name: /library/i })).toBeVisible()
expect(screen.getByLabelText(/style/i)).toHaveValue('botanical-press')
```

- [ ] **Step 2: Run the focused test suite and the web build**

Run:
`npm run test:run`
`npm run build`

Expected:
- all UI tests pass
- TypeScript compilation passes
- Vite build completes without theme or route errors

- [ ] **Step 3: Verify the shell in a browser at desktop and mobile widths**

Open the local web app and confirm:

- the bottom nav stays visible
- the active route is obvious
- switching styles changes the app shell immediately
- Botanical Press is still the default after refresh

- [ ] **Step 4: Commit the verification and docs update**

```bash
git add src/Librory.Web/package.json src/Librory.Web/vitest.config.ts src/Librory.Web/src/test src/Librory.Web/src/*.test.tsx
git commit -m "test: cover theme shell and navigation"
```

## Coverage Check

The plan covers every requirement from `docs/story-09-frontend-shell-theme-switching.md`:

- app shell and bottom navigation: Task 1
- four placeholder pages: Task 1
- settings page with style dropdown: Task 3
- local persistence: Task 2
- shared base tokens and per-theme overrides: Task 2
- Botanical Press default: Task 2
- UI verification and build validation: Task 4

## Notes

- Keep the first pass narrow: page shell, theme tokens, and a single settings control.
- Design mobile-first, but ensure the shell remains usable on desktop widths.
- Do not add backend calls for theme storage yet.
- Do not widen the settings page into a full preferences center until the shell is stable.
