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
              family: { id: 'family-1', name: 'The Yans', memberId: 'member-1', memberCount: 1 },
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

  it('switches families and creates a placeholder member', async () => {
    const user = userEvent.setup()
    let memberList = [
      { memberId: 'member-1', displayName: 'Alice', role: 'Admin', preferredLanguage: 0, isActive: true, hasAccount: true },
    ]
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input)
      if (url === '/api/families') {
        return new Response(JSON.stringify([
          { familyId: 'family-1', familyName: 'The Yans', memberId: 'member-1', memberDisplayName: 'Alice', role: 'Admin', isActive: true },
          { familyId: 'family-2', familyName: 'Reading Club', memberId: 'member-2', memberDisplayName: 'Alice', role: 'Admin', isActive: true },
        ]), { status: 200 })
      }
      if (url === '/api/family/current/members' && init?.method === 'POST') {
        const created = { memberId: 'member-3', displayName: 'Mia', role: 'Member', preferredLanguage: 1, isActive: true, hasAccount: false }
        memberList = [...memberList, created]
        return new Response(JSON.stringify(created), { status: 201 })
      }
      if (url === '/api/family/current/members/member-1/recommendation-profile') {
        return new Response(JSON.stringify({ memberId: 'member-1', minimumAge: 8, maximumAge: 12, favoriteAuthors: [], excludedAuthors: [], favoriteGenres: [], excludedGenres: [], favoriteStyles: [], excludedStyles: [], preferredBookLanguages: [0], preferenceNotes: null, profileVisibility: 0, useInFamilyRecommendations: true }), { status: 200 })
      }
      if (url === '/api/family/current/members') {
        return new Response(JSON.stringify(memberList), { status: 200 })
      }
      if (url === '/api/families/family-2/select') {
        return new Response(JSON.stringify({ familyId: 'family-2', familyName: 'Reading Club', memberId: 'member-2', memberDisplayName: 'Alice', role: 'Member', isActive: true }), { status: 200 })
      }
      if (url === '/api/family/current' && init?.credentials === 'include') {
        return new Response(JSON.stringify({ familyId: 'family-2', familyName: 'Reading Club', memberId: 'member-2', memberDisplayName: 'Alice', memberRole: 'Admin', memberCount: 1 }), { status: 200 })
      }
      throw new Error(`Unexpected fetch request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(
      <MemoryRouter initialEntries={['/app/settings']}>
        <ThemeRoot>
          <AuthSessionProvider
            initialSession={{
              status: 'authenticated',
              user: { id: 'member-1', displayName: 'Alice', role: 'Admin' },
              family: { id: 'family-1', name: 'The Yans', memberId: 'member-1', memberCount: 1 },
            }}
          >
            <SettingsPage />
          </AuthSessionProvider>
        </ThemeRoot>
      </MemoryRouter>,
    )

    expect(await screen.findByText('Reading preferences')).toBeVisible()
    expect(fetchMock).toHaveBeenCalledWith('/api/family/current/members/member-1/recommendation-profile', { credentials: 'include' })

    await user.selectOptions(await screen.findByLabelText(/current family/i), 'family-2')
    expect(fetchMock).toHaveBeenCalledWith('/api/families/family-2/select', expect.objectContaining({ method: 'POST' }))
    expect(await screen.findByText(/reading club/i)).toBeVisible()

    await user.type(screen.getByLabelText(/add a placeholder member/i), 'Mia')
    await user.click(screen.getByRole('button', { name: /add member/i }))
    expect(await screen.findByText('Mia')).toBeVisible()
  })

  it('creates an invitation and displays its one-time URL', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input)
      if (url === '/api/families') return new Response(JSON.stringify([{ familyId: 'family-1', familyName: 'The Yans', memberId: 'member-1', memberDisplayName: 'Alice', role: 'Admin', isActive: true }]), { status: 200 })
      if (url === '/api/family/current/members') return new Response(JSON.stringify([]), { status: 200 })
      if (url === '/api/family/current/invitations' && init?.method === 'POST') return new Response(JSON.stringify({ invitationId: 'invite-1', familyId: 'family-1', targetMemberId: null, email: 'bob@example.com', status: 'Pending', createdAt: '2026-08-06T00:00:00Z', expiresAt: '2026-08-13T00:00:00Z', invitationUrl: '/family-invitations/secret-token' }), { status: 201 })
      if (url === '/api/family/current/invitations') return new Response(JSON.stringify([]), { status: 200 })
      throw new Error(`Unexpected fetch request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)
    vi.stubGlobal('navigator', { clipboard: { writeText: vi.fn(async () => undefined) } })

    render(
      <MemoryRouter initialEntries={['/app/settings']}>
        <ThemeRoot>
        <AuthSessionProvider initialSession={{ status: 'authenticated', user: { id: 'member-1', displayName: 'Alice', role: 'Admin' }, family: { id: 'family-1', name: 'The Yans', memberId: 'member-1', memberCount: 1 } }}>
            <SettingsPage />
          </AuthSessionProvider>
        </ThemeRoot>
      </MemoryRouter>,
    )

    await user.type(await screen.findByLabelText(/invitee email/i), 'bob@example.com')
    await user.click(screen.getByRole('button', { name: /send invitation/i }))

    expect(await screen.findByText('/family-invitations/secret-token')).toBeVisible()
    expect(screen.getByRole('button', { name: /copy invitation link/i })).toBeVisible()
  })
})
