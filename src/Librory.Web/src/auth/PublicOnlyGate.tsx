import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { AuthLoading } from './AuthLoading'
import { useAuthSession } from './AuthSessionContext'

export function PublicOnlyGate({ children }: { children: ReactNode }) {
  const { status } = useAuthSession()

  if (status === 'loading') {
    return <AuthLoading />
  }

  if (status === 'authenticated') {
    return <Navigate to="/app/home" replace />
  }

  return <>{children}</>
}
