import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { vi } from 'vitest'
import App from '@/App'
import { AuthSessionProvider } from '@/auth/AuthSessionContext'
import LoginPage from './LoginPage'

describe('LoginPage', () => {
  it('shows the three sign in choices', () => {
    render(
      <MemoryRouter>
        <AuthSessionProvider initialSession={{ status: 'anonymous', user: null, family: null }}>
          <LoginPage />
        </AuthSessionProvider>
      </MemoryRouter>,
    )

    expect(screen.getByRole('heading', { name: /sign in/i })).toBeVisible()
    expect(screen.getByRole('button', { name: /continue with google/i })).toBeVisible()
    expect(screen.getByRole('button', { name: /continue with microsoft/i })).toBeVisible()
    expect(screen.getByRole('button', { name: /continue with email/i })).toBeVisible()
  })

  it('starts a dev login flow when a provider button is clicked', async () => {
    const user = userEvent.setup()

    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input)

        if (url === '/dev/auth/login') {
          return new Response(
            JSON.stringify({
              familyId: 'family-1',
              familyName: 'Google Family',
              memberId: 'member-1',
              memberDisplayName: 'Google Admin',
              memberRole: 1,
              preferredLanguage: 0,
            }),
            {
              status: 200,
              headers: { 'Content-Type': 'application/json' },
            },
          )
        }

        if (url === '/api/family/current') {
          return new Response(
            JSON.stringify({
              familyId: 'family-1',
              familyName: 'Google Family',
              memberId: 'member-1',
              memberDisplayName: 'Google Admin',
              memberCount: 1,
              bookCount: 0,
              wishlistCount: 0,
            }),
            {
              status: 200,
              headers: { 'Content-Type': 'application/json' },
            },
          )
        }

        return new Response('', { status: 404 })
      }),
    )

    render(
      <MemoryRouter initialEntries={['/login']}>
        <AuthSessionProvider initialSession={{ status: 'anonymous', user: null, family: null }}>
          <App />
        </AuthSessionProvider>
      </MemoryRouter>,
    )

    await user.click(screen.getByRole('button', { name: /continue with google/i }))

    expect(await screen.findByRole('button', { name: /scan a shelf/i })).toBeVisible()
    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/dev/auth/login',
      expect.objectContaining({
        method: 'POST',
      }),
    )
  })
})
