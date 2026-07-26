import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuthSession } from './AuthSessionContext'

export function AuthGate({ children }: { children: ReactNode }) {
  const { status } = useAuthSession()
  const location = useLocation()

  if (status === 'loading') {
    return (
      <div aria-live="polite" role="status" className="p-4 text-sm text-[var(--text-secondary)]">
        Loading...
      </div>
    )
  }

  if (status === 'anonymous') {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  return <>{children}</>
}
