import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import App from './App'
import { ThemeRoot } from '@/theme/ThemeRoot'
import { AuthSessionProvider } from '@/auth/AuthSessionContext'

describe('App shell', () => {
  it('shows the settings page and bottom navigation on the settings route', () => {
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

    expect(screen.getByRole('heading', { name: /settings/i })).toBeVisible()
    expect(screen.getByRole('link', { name: /home/i })).toBeVisible()
    expect(screen.getByRole('link', { name: /scans/i })).toBeVisible()
    expect(screen.getByRole('link', { name: /library/i })).toBeVisible()
  })
})
