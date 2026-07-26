import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuthSession } from './AuthSessionContext'

export function PublicOnlyGate({ children }: { children: ReactNode }) {
  const { status } = useAuthSession()

  if (status === 'loading') {
    return <div className="p-4 text-sm text-[var(--text-secondary)]">Loading...</div>
  }

  if (status === 'authenticated') {
    return <Navigate to="/app/home" replace />
  }

  return <>{children}</>
}
