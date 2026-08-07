import { render, screen } from '@testing-library/react'
import { MemoryRouter, useLocation } from 'react-router-dom'
import App from '@/App'
import { AuthSessionProvider } from './AuthSessionContext'

function LocationProbe() {
  const location = useLocation()

  return <div>{location.pathname}</div>
}

describe('Auth boundary', () => {
  it('redirects anonymous users from protected routes to the login page', async () => {
    render(
      <MemoryRouter initialEntries={['/app/home']}>
        <AuthSessionProvider initialSession={{ status: 'anonymous', user: null, family: null }}>
          <LocationProbe />
          <App />
        </AuthSessionProvider>
      </MemoryRouter>,
    )

    expect(await screen.findByText('/login')).toBeVisible()
  })

  it('redirects authenticated users away from public pages and into the app home', async () => {
    render(
      <MemoryRouter initialEntries={['/login']}>
        <AuthSessionProvider
          initialSession={{
            status: 'authenticated',
            user: { id: 'user-1', displayName: 'Alice', email: 'alice@example.com' },
            family: { id: 'family-1', name: 'The Yans', memberId: 'member-1', memberCount: 1 },
          }}
        >
          <LocationProbe />
          <App />
        </AuthSessionProvider>
      </MemoryRouter>,
    )

    expect(await screen.findByText('/app/home')).toBeVisible()
  })
})
