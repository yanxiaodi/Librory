import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'
import { authEndpoints } from './authEndpoints'
import type { AuthSession } from './authSessionTypes'

type DevLoginRequest = {
  familyName: string
  memberDisplayName: string
  preferredLanguage: number
}

type CurrentFamilyResponse = {
  familyId: string
  familyName: string
  memberId: string
  memberDisplayName: string
  memberCount: number
  memberRole?: string
}

const anonymousSession: AuthSession = {
  status: 'anonymous',
  user: null,
  family: null,
}

const loadingSession: AuthSession = {
  status: 'loading',
  user: null,
  family: null,
}

type AuthSessionContextValue = {
  session: AuthSession
  refreshSession: () => Promise<void>
  signInWithDevLogin: (request: DevLoginRequest) => Promise<void>
  signOut: (options?: { afterSignOut?: () => void }) => Promise<void>
}

const AuthSessionContext = createContext<AuthSessionContextValue | null>(null)

export function AuthSessionProvider({
  children,
  initialSession,
}: {
  children: ReactNode
  initialSession?: AuthSession
}) {
  const hasInitialSession = initialSession !== undefined
  const [session, setSession] = useState<AuthSession>(hasInitialSession ? initialSession : loadingSession)

  const refreshSession = useCallback(async () => {
    try {
      const response = await fetch('/api/family/current', {
        credentials: 'include',
      })

      if (!response.ok) {
        setSession(anonymousSession)
        return
      }

      const currentFamily = (await response.json()) as CurrentFamilyResponse

      setSession({
        status: 'authenticated',
        user: {
          id: currentFamily.memberId,
          displayName: currentFamily.memberDisplayName,
          role: currentFamily.memberRole,
        },
        family: {
          id: currentFamily.familyId,
          name: currentFamily.familyName,
          memberId: currentFamily.memberId,
          memberCount: currentFamily.memberCount,
        },
      })
    } catch {
      setSession(anonymousSession)
    }
  }, [])

  const signInWithDevLogin = useCallback(async (request: DevLoginRequest) => {
    setSession(loadingSession)

    const response = await fetch('/dev/auth/login', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      credentials: 'include',
      body: JSON.stringify(request),
    })

    if (!response.ok) {
      setSession(anonymousSession)
      throw new Error('Development login failed.')
    }

    await refreshSession()
  }, [refreshSession])

  const signOut = useCallback(async (options?: { afterSignOut?: () => void }) => {
    setSession(loadingSession)

    await fetch(authEndpoints.logout, {
      method: 'POST',
      credentials: 'include',
    })

    options?.afterSignOut?.()
    await Promise.resolve()
    setSession(anonymousSession)
  }, [])

  useEffect(() => {
    if (hasInitialSession) {
      setSession(initialSession ?? anonymousSession)
      return
    }

    void refreshSession()
  }, [hasInitialSession, initialSession, refreshSession])

  return (
    <AuthSessionContext.Provider
      value={{
        session,
        refreshSession,
        signInWithDevLogin,
        signOut,
      }}
    >
      {children}
    </AuthSessionContext.Provider>
  )
}

export function useAuthSession(): AuthSession {
  const context = useContext(AuthSessionContext)

  if (context === null) {
    return anonymousSession
  }

  return context.session
}

export function useAuthSessionActions() {
  const context = useContext(AuthSessionContext)

  if (context === null) {
    throw new Error('useAuthSessionActions must be used within AuthSessionProvider.')
  }

  return {
    refreshSession: context.refreshSession,
    signInWithDevLogin: context.signInWithDevLogin,
    signOut: context.signOut,
  }
}
