import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { vi } from 'vitest'
import App from '@/App'
import { AuthSessionProvider } from '@/auth/AuthSessionContext'
import SettingsPage from './SettingsPage'
import { ThemeRoot } from '@/theme/ThemeRoot'

describe('SettingsPage', () => {
  it('changes theme selection from the style dropdown', async () => {
    const user = userEvent.setup()

    render(
      <MemoryRouter>
        <ThemeRoot>
          <AuthSessionProvider
            initialSession={{
              status: 'anonymous',
              user: null,
              family: null,
            }}
          >
            <SettingsPage />
          </AuthSessionProvider>
        </ThemeRoot>
      </MemoryRouter>,
    )

    await user.click(screen.getByRole('button', { name: /botanical press/i }))
    await user.click(screen.getByRole('option', { name: /cozy archive/i }))

    expect(screen.getByRole('button', { name: /cozy archive/i })).toBeVisible()
  })

  it('logs out and returns to the landing page', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      expect(String(input)).toBe('/auth/logout')
      return new Response(null, { status: 204 })
    })

    vi.stubGlobal('fetch', fetchMock)

    render(
      <MemoryRouter initialEntries={['/app/settings']}>
        <ThemeRoot>
          <AuthSessionProvider
            initialSession={{
              status: 'authenticated',
              user: { id: 'user-1', displayName: 'Alice', email: 'alice@example.com' },
              family: { id: 'family-1', name: 'The Yans', memberCount: 1 },
            }}
          >
            <App />
          </AuthSessionProvider>
        </ThemeRoot>
      </MemoryRouter>,
    )

    await user.click(screen.getByRole('button', { name: /sign out/i }))

    expect(await screen.findByRole('heading', { name: /sign in/i })).toBeVisible()
    expect(fetchMock).toHaveBeenCalledWith(
      '/auth/logout',
      expect.objectContaining({
        method: 'POST',
        credentials: 'include',
      }),
    )
  })
})
