import { NavLink, Route, Routes, useLocation } from 'react-router-dom'
import { BookOpen, ScanSearch, LibraryBig, Settings2 } from 'lucide-react'
import { HomePage } from '@/pages/HomePage'
import { ScansPage } from '@/pages/ScansPage'
import { LibraryPage } from '@/pages/LibraryPage'
import SettingsPage from '@/pages/SettingsPage'
import { cn } from '@/lib/utils'

const navigationItems = [
  { to: '/', label: 'Home', icon: BookOpen },
  { to: '/scans', label: 'Scans', icon: ScanSearch },
  { to: '/library', label: 'Library', icon: LibraryBig },
  { to: '/settings', label: 'Settings', icon: Settings2 },
] as const

const pageTitles: Record<string, string> = {
  '/': 'Home',
  '/scans': 'Scans',
  '/library': 'Library',
  '/settings': 'Settings',
}

export default function App() {
  const location = useLocation()
  const title = pageTitles[location.pathname] ?? 'Librory'

  return (
    <div className="mx-auto flex min-h-screen w-full max-w-[430px] flex-col bg-[var(--page-bg)] text-[var(--text-primary)] shadow-[0_0_0_1px_var(--border-subtle),0_20px_64px_rgba(58,48,42,0.14)] sm:max-w-[460px] md:my-4 md:min-h-[calc(100vh-2rem)] md:rounded-[36px]">
      <header className="px-5 pb-5 pt-6">
        <p className="text-[11px] font-semibold uppercase tracking-[0.24em] text-[var(--text-tertiary)]">Librory</p>
        <h1 className="mt-2 font-[family-name:var(--font-display)] text-[2rem] font-normal italic tracking-[-0.01em] text-[var(--text-primary)]">
          {title}
        </h1>
      </header>

      <main className="flex-1 px-4 pb-24">
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/scans" element={<ScansPage />} />
          <Route path="/library" element={<LibraryPage />} />
          <Route path="/settings" element={<SettingsPage />} />
        </Routes>
      </main>

      <nav
        aria-label="Primary"
        className="border-t border-[var(--border-subtle)] bg-[var(--page-bg)] px-2 pb-[calc(env(safe-area-inset-bottom)+0.5rem)] pt-2"
      >
        <div className="grid grid-cols-4">
          {navigationItems.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                cn(
                  'flex flex-col items-center gap-1 px-2 py-2 text-[11px] font-medium transition',
                  isActive
                    ? 'text-[var(--accent)]'
                    : 'text-[var(--text-secondary)]',
                )
              }
            >
              <Icon className="h-5 w-5" />
              <span>{label}</span>
            </NavLink>
          ))}
        </div>
        <div className="mt-1 flex justify-center">
          <div className="h-1.5 w-32 rounded-full bg-[var(--text-primary)]/85" />
        </div>
      </nav>
    </div>
  )
}
