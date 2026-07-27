import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { AuthSessionProvider } from '@/auth/AuthSessionContext'
import LoginPage from './LoginPage'

describe('LoginPage', () => {
  it('shows the real provider links', () => {
    render(
      <MemoryRouter>
        <AuthSessionProvider initialSession={{ status: 'anonymous', user: null, family: null }}>
          <LoginPage />
        </AuthSessionProvider>
      </MemoryRouter>,
    )

    expect(screen.getByRole('heading', { name: /sign in/i })).toBeVisible()
    expect(screen.getByRole('link', { name: /continue with google/i })).toHaveAttribute(
      'href',
      '/auth/google/start',
    )
    expect(screen.getByRole('link', { name: /continue with microsoft/i })).toHaveAttribute(
      'href',
      '/auth/microsoft/start',
    )
    expect(screen.queryByRole('link', { name: /continue with email/i })).not.toBeInTheDocument()
  })
})
