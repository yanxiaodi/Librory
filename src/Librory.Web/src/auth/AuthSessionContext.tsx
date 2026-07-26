import { createContext, type ReactNode, useContext } from 'react'
import type { AuthSession } from './authSessionTypes'

const anonymousSession: AuthSession = {
  status: 'anonymous',
  user: null,
  family: null,
}

const AuthSessionContext = createContext<AuthSession>(anonymousSession)

export function AuthSessionProvider({
  children,
  initialSession = anonymousSession,
}: {
  children: ReactNode
  initialSession?: AuthSession
}) {
  return <AuthSessionContext.Provider value={initialSession}>{children}</AuthSessionContext.Provider>
}

export function useAuthSession(): AuthSession {
  return useContext(AuthSessionContext)
}
