import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  createInvitation,
  listFamilies,
  revokeInvitation,
  setMemberActive,
} from './familyApi'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('familyApi', () => {
  it('lists families with the authenticated session cookie', async () => {
    const fetchMock = vi.fn(async () =>
      new Response(JSON.stringify([{ familyId: 'family-1', familyName: 'The Yans', memberId: 'member-1', memberDisplayName: 'Alice', role: 'Admin', isActive: true }]), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    await expect(listFamilies()).resolves.toEqual([
      { familyId: 'family-1', familyName: 'The Yans', memberId: 'member-1', memberDisplayName: 'Alice', role: 'Admin', isActive: true },
    ])
    expect(fetchMock).toHaveBeenCalledWith('/api/families', { credentials: 'include' })
  })

  it('creates an invitation with a JSON body and credentials', async () => {
    const fetchMock = vi.fn(async () =>
      new Response(JSON.stringify({ invitationId: 'invite-1', email: 'bob@example.com', status: 'Pending', invitationUrl: '/family-invitations/token' }), {
        status: 201,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    await createInvitation({ email: 'bob@example.com' })

    expect(fetchMock).toHaveBeenCalledWith('/api/family/current/invitations', expect.objectContaining({
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: 'bob@example.com' }),
    }))
  })

  it('uses the active-state endpoint for deactivation', async () => {
    const fetchMock = vi.fn(async () => new Response(JSON.stringify({ memberId: 'member-1', isActive: false }), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    await setMemberActive('member-1', false)

    expect(fetchMock).toHaveBeenCalledWith('/api/family/current/members/member-1/deactivate', expect.objectContaining({
      method: 'POST',
      credentials: 'include',
    }))
  })

  it('throws a status-bearing error for failed requests', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify({ title: 'Forbidden' }), { status: 403 })))

    await expect(revokeInvitation('invite-1')).rejects.toThrow('Family API request failed (403): Forbidden')
  })
})
