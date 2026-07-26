import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { AuthLoading } from './AuthLoading'
import { useAuthSession } from './AuthSessionContext'

export function AuthGate({ children }: { children: ReactNode }) {
  const { status } = useAuthSession()
  const location = useLocation()

  if (status === 'loading') {
    return <AuthLoading />
  }

  if (status === 'anonymous') {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  return <>{children}</>
}
