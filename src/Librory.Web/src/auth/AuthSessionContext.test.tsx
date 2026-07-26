import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthSessionProvider, useAuthSession, useAuthSessionActions } from './AuthSessionContext'

function AuthSessionProbe() {
  const session = useAuthSession()
  const { refreshSession, signInWithDevLogin, signOut } = useAuthSessionActions()

  return (
    <div>
      <div data-testid="status">{session.status}</div>
      <div data-testid="family">{session.family?.name ?? 'none'}</div>
      <button type="button" onClick={() => void refreshSession()}>
        Refresh session
      </button>
      <button
        type="button"
        onClick={() =>
          void signInWithDevLogin({
            familyName: 'Google Family',
            memberDisplayName: 'Google Admin',
            preferredLanguage: 0,
          })
        }
      >
        Sign in
      </button>
      <button type="button" onClick={() => void signOut()}>
        Sign out
      </button>
    </div>
  )
}

function renderProbe(initialSession?: Parameters<typeof AuthSessionProvider>[0]['initialSession']) {
  return render(
    <AuthSessionProvider initialSession={initialSession}>
      <AuthSessionProbe />
    </AuthSessionProvider>,
  )
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('AuthSessionContext', () => {
  it('refreshes the current family session from the API', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL) => {
        expect(String(input)).toBe('/api/family/current')

        return new Response(
          JSON.stringify({
            familyId: 'family-1',
            familyName: 'The Yans',
            memberId: 'member-1',
            memberDisplayName: 'Alice',
            memberCount: 2,
          }),
          {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          },
        )
      }),
    )

    const user = userEvent.setup()
    renderProbe({ status: 'anonymous', user: null, family: null })

    await user.click(screen.getByRole('button', { name: /refresh session/i }))

    expect(await screen.findByText('authenticated')).toBeVisible()
    expect(screen.getByText('The Yans')).toBeVisible()
  })

  it('starts a dev login flow and refreshes the hydrated session', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input)

      if (url === '/dev/auth/login') {
        return new Response('', { status: 200 })
      }

      if (url === '/api/family/current') {
        return new Response(
          JSON.stringify({
            familyId: 'family-2',
            familyName: 'Google Family',
            memberId: 'member-2',
            memberDisplayName: 'Google Admin',
            memberCount: 1,
          }),
          {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          },
        )
      }

      return new Response('', { status: 404 })
    })

    vi.stubGlobal('fetch', fetchMock)

    const user = userEvent.setup()
    renderProbe({ status: 'anonymous', user: null, family: null })

    await user.click(screen.getByRole('button', { name: /sign in/i }))

    expect(await screen.findByText('authenticated')).toBeVisible()
    expect(screen.getByText('Google Family')).toBeVisible()
    expect(fetchMock).toHaveBeenCalledWith(
      '/dev/auth/login',
      expect.objectContaining({
        method: 'POST',
      }),
    )
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/family/current',
      expect.objectContaining({
        credentials: 'include',
      }),
    )
  })

  it('signs out through the dev logout endpoint', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      expect(String(input)).toBe('/dev/auth/logout')

      return new Response('', { status: 200 })
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
      '/dev/auth/logout',
      expect.objectContaining({
        method: 'POST',
      }),
    )
    expect(await screen.findByText('anonymous')).toBeVisible()
  })
})
