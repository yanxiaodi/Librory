import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import App from '@/App'
import { AuthSessionProvider } from '@/auth/AuthSessionContext'

describe('HomePage', () => {
  it('shows the scan-first home screen for an authenticated family', () => {
    render(
      <MemoryRouter initialEntries={['/app/home']}>
        <AuthSessionProvider
          initialSession={{
            status: 'authenticated',
            user: { id: 'user-1', displayName: 'Alice', email: 'alice@example.com' },
            family: { id: 'family-1', name: 'The Yans', memberId: 'member-1', memberCount: 1 },
          }}
        >
          <App />
        </AuthSessionProvider>
      </MemoryRouter>,
    )

    expect(screen.getByRole('link', { name: /scan a shelf/i })).toBeVisible()
    expect(screen.getByRole('link', { name: /browse library/i })).toBeVisible()
    expect(screen.getByText(/^recent scans$/i)).toBeVisible()
    expect(screen.getByText(/your family's reading companion/i)).toBeVisible()
  })
})
