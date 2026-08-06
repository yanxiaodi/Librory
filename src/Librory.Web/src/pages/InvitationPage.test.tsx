import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthSessionProvider } from '@/auth/AuthSessionContext'
import InvitationPage from './InvitationPage'

afterEach(() => vi.unstubAllGlobals())

const preview = {
  id: 'invite-1',
  familyName: 'The Yans',
  email: 'bob@example.com',
  targetMemberId: null,
  expiresAt: '2026-08-13T00:00:00Z',
}

describe('InvitationPage', () => {
  it('shows provider links that return to the invitation after login', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify(preview), { status: 200 })))

    render(
      <MemoryRouter initialEntries={['/family-invitations/token-1']}>
        <AuthSessionProvider initialSession={{ status: 'anonymous', user: null, family: null }}>
          <Routes><Route path="/family-invitations/:token" element={<InvitationPage />} /><Route path="/app/home" element={<div>Home destination</div>} /></Routes>
        </AuthSessionProvider>
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: /join the yans/i })).toBeVisible()
    expect(screen.getByRole('link', { name: /continue with google/i })).toHaveAttribute('href', '/auth/google/start?returnUrl=%2Ffamily-invitations%2Ftoken-1')
    expect(screen.getByText(/bob@example.com/i)).toBeVisible()
  })

  it('accepts the invitation, selects the family, and navigates home', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input)
      if (url === '/api/family-invitations/token-1') return new Response(JSON.stringify(preview), { status: 200 })
      if (url === '/api/family-invitations/token-1/accept') return new Response(JSON.stringify({ familyId: 'family-2', familyName: 'The Yans', memberId: 'member-2', memberDisplayName: 'Bob', role: 'Member', isActive: true }), { status: 200 })
      if (url === '/api/families/family-2/select') return new Response(JSON.stringify({ familyId: 'family-2', familyName: 'The Yans', memberId: 'member-2', memberDisplayName: 'Bob', role: 'Member', isActive: true }), { status: 200 })
      if (url === '/api/family/current' && init?.credentials === 'include') return new Response(JSON.stringify({ familyId: 'family-2', familyName: 'The Yans', memberId: 'member-2', memberDisplayName: 'Bob', memberRole: 'Member', memberCount: 2 }), { status: 200 })
      throw new Error(`Unexpected fetch request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(
      <MemoryRouter initialEntries={['/family-invitations/token-1']}>
        <AuthSessionProvider initialSession={{ status: 'authenticated', user: { id: 'member-1', displayName: 'Bob', email: 'bob@example.com' }, family: { id: 'family-1', name: 'Bob Family', memberCount: 1 } }}>
          <Routes><Route path="/family-invitations/:token" element={<InvitationPage />} /><Route path="/app/home" element={<div>Home destination</div>} /></Routes>
        </AuthSessionProvider>
      </MemoryRouter>,
    )

    await user.click(await screen.findByRole('button', { name: /accept invitation/i }))

    expect(fetchMock).toHaveBeenCalledWith('/api/family-invitations/token-1/accept', expect.objectContaining({ method: 'POST', credentials: 'include' }))
    expect(await screen.findByText(/home destination/i)).toBeVisible()
  })
})
