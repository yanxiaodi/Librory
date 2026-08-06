import { NavLink, Navigate, Outlet, Route, Routes, useLocation } from 'react-router-dom'
import { BookOpen, ScanSearch, LibraryBig, Settings2 } from 'lucide-react'
import { HomePage } from '@/pages/HomePage'
import { LandingPage } from '@/pages/LandingPage'
import LoginPage from '@/pages/LoginPage'
import { ScansPage } from '@/pages/ScansPage'
import { LibraryPage } from '@/pages/LibraryPage'
import SettingsPage from '@/pages/SettingsPage'
import InvitationPage from '@/pages/InvitationPage'
import { cn } from '@/lib/utils'
import { AuthGate } from '@/auth/AuthGate'
import { PublicOnlyGate } from '@/auth/PublicOnlyGate'

const navigationItems = [
  { to: '/app/home', label: 'Home', icon: BookOpen },
  { to: '/app/scans', label: 'Scans', icon: ScanSearch },
  { to: '/app/library', label: 'Library', icon: LibraryBig },
  { to: '/app/settings', label: 'Settings', icon: Settings2 },
] as const

const pageTitles: Record<string, string> = {
  '/app/home': 'Today’s shelf',
  '/app/scans': 'Scans',
  '/app/library': 'Library',
  '/app/settings': 'Settings',
}

function AuthenticatedShell() {
  const location = useLocation()
  const title = pageTitles[location.pathname] ?? 'Librory'

  return (
    <div className="flex h-[100dvh] items-center justify-center overflow-hidden bg-[var(--page-bg)] md:px-4 md:py-4">
      <div className="relative flex h-[100dvh] w-full max-w-[430px] flex-col overflow-hidden bg-[var(--page-bg)] text-[var(--text-primary)] shadow-[0_0_0_1px_var(--border-subtle),0_16px_36px_rgba(58,48,42,0.08)] sm:max-w-[460px] md:h-[900px] md:max-h-[calc(100dvh-2rem)] md:rounded-[24px]">
        {/* === Fixed Header === */}
        <header className="shrink-0 bg-[var(--page-bg)] px-5 pb-4 pt-6">
          <p className="text-[11px] font-semibold uppercase tracking-[0.24em] text-[var(--text-tertiary)]">Librory</p>
          <h1 className="mt-2 font-[family-name:var(--font-display)] text-[1.9rem] font-normal italic tracking-[-0.01em] text-[var(--text-primary)]">
            {title}
          </h1>
        </header>

        {/* === Scrollable Content === */}
        <main className="flex-1 overflow-y-auto px-4">
          <Outlet />
        </main>

        {/* === Fixed Bottom Nav === */}
        <nav
          aria-label="Primary"
          className="shrink-0 border-t border-[var(--border-subtle)] bg-[var(--page-bg)] px-2 pb-[calc(env(safe-area-inset-bottom)+0.75rem)] pt-2"
        >
          <div className="grid grid-cols-4">
            {navigationItems.map(({ to, label, icon: Icon }) => (
              <NavLink
                key={to}
                to={to}
                className={({ isActive }) =>
                  cn(
                    'flex flex-col items-center gap-1.5 px-2 py-2 text-[11px] font-medium transition',
                    isActive ? 'text-[var(--accent)]' : 'text-[var(--text-secondary)]',
                  )
                }
              >
                <Icon className="h-5 w-5" strokeWidth={1.5} />
                <span>{label}</span>
              </NavLink>
            ))}
          </div>
          <div className="mt-1 flex justify-center">
            <div className="h-1.5 w-32 rounded-full bg-[var(--text-primary)]/85" />
          </div>
        </nav>
      </div>
    </div>
  )
}

export default function App() {
  return (
    <Routes>
      <Route
        path="/"
        element={
          <PublicOnlyGate>
            <LandingPage />
          </PublicOnlyGate>
        }
      />
      <Route
        path="/login"
        element={
          <PublicOnlyGate>
            <LoginPage />
          </PublicOnlyGate>
        }
      />
      <Route
        path="/app"
        element={
          <AuthGate>
            <AuthenticatedShell />
          </AuthGate>
        }
      >
        <Route index element={<Navigate to="home" replace />} />
        <Route path="home" element={<HomePage />} />
        <Route path="scans" element={<ScansPage />} />
        <Route path="library" element={<LibraryPage />} />
        <Route path="settings" element={<SettingsPage />} />
      </Route>
      <Route path="/family-invitations/:token" element={<InvitationPage />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
