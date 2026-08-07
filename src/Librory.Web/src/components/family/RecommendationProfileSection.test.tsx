import { afterEach, describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { RecommendationProfileSection } from './RecommendationProfileSection'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('RecommendationProfileSection', () => {
  it('loads a selected member profile into the form', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input)
      if (url === '/api/family/current/members') {
        return new Response(JSON.stringify([
          { memberId: 'member-1', displayName: 'Alice', role: 'Admin', preferredLanguage: 0, isActive: true, hasAccount: true },
        ]), { status: 200 })
      }
      if (url === '/api/family/current/members/member-1/recommendation-profile') {
        return new Response(JSON.stringify({
          memberId: 'member-1',
          minimumAge: 8,
          maximumAge: 12,
          favoriteAuthors: ['Roald Dahl'],
          excludedAuthors: [],
          favoriteGenres: ['Fantasy'],
          excludedGenres: [],
          favoriteStyles: [],
          excludedStyles: [],
          preferredBookLanguages: [0],
          preferenceNotes: 'Enjoys imaginative stories.',
          profileVisibility: 'Family',
          useInFamilyRecommendations: true,
        }), { status: 200 })
      }
      throw new Error(`Unexpected fetch request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(<RecommendationProfileSection isAdmin currentMemberId="member-1" />)

    expect(await screen.findByDisplayValue('8')).toBeVisible()
    expect(screen.getByDisplayValue('12')).toBeVisible()
    expect(screen.getByDisplayValue('Enjoys imaginative stories.')).toBeVisible()
    expect(screen.getByRole('combobox', { name: /profile visibility/i })).toHaveValue('Family')
    expect(screen.getByLabelText(/minimum reading age/i)).toHaveAttribute('type', 'number')
    expect(screen.getByLabelText(/minimum reading age/i)).toHaveAttribute('inputmode', 'numeric')
  })

  it('rejects invalid reading ages instead of silently clearing them', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      if (String(input) === '/api/family/current/members') {
        return new Response(JSON.stringify([{ memberId: 'member-1', displayName: 'Alice', role: 'Admin', preferredLanguage: 0, isActive: true, hasAccount: true }]), { status: 200 })
      }
      if (init?.method === 'PUT') return new Response(JSON.stringify({ memberId: 'member-1' }), { status: 200 })
      return new Response(null, { status: 404 })
    })
    vi.stubGlobal('fetch', fetchMock)

    render(<RecommendationProfileSection isAdmin currentMemberId="member-1" />)

    const minimumAge = await screen.findByLabelText(/minimum reading age/i)
    fireEvent.change(minimumAge, { target: { value: '1.5' } })
    fireEvent.submit(screen.getByRole('button', { name: /save preferences/i }).closest('form')!)

    expect(await screen.findByRole('alert')).toHaveTextContent(/whole numbers/i)
    expect(fetchMock.mock.calls.some(([, init]) => init?.method === 'PUT')).toBe(false)
  })

  it('shows a member loading error instead of an empty-family message', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(null, { status: 500 })))

    render(<RecommendationProfileSection isAdmin currentMemberId="member-1" />)

    expect(await screen.findByRole('alert')).toHaveTextContent(/unable to load recommendation members/i)
    expect(screen.queryByText(/no active family members/i)).not.toBeInTheDocument()
  })

  it('creates an empty profile after a 404 and sends explicit clears', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input)
      if (url === '/api/family/current/members') {
        return new Response(JSON.stringify([{ memberId: 'member-1', displayName: 'Alice', role: 'Admin', preferredLanguage: 0, isActive: true, hasAccount: true }]), { status: 200 })
      }
      if (init?.method === 'PUT') {
        return new Response(JSON.stringify({ memberId: 'member-1', minimumAge: null, maximumAge: null, favoriteAuthors: [], excludedAuthors: [], favoriteGenres: [], excludedGenres: [], favoriteStyles: [], excludedStyles: [], preferredBookLanguages: [], preferenceNotes: 'Enjoys stories.', profileVisibility: 'Family', useInFamilyRecommendations: true }), { status: 200 })
      }
      return new Response(null, { status: 404 })
    })
    vi.stubGlobal('fetch', fetchMock)

    render(<RecommendationProfileSection isAdmin currentMemberId="member-1" />)

    const notes = await screen.findByLabelText(/preference notes/i)
    await user.type(notes, 'Enjoys stories.')
    await user.click(screen.getByRole('button', { name: /save preferences/i }))

    expect(await screen.findByRole('status')).toHaveTextContent(/saved/i)
    const saveCall = fetchMock.mock.calls.find(([, init]) => init?.method === 'PUT')
    expect(saveCall?.[1]).toEqual(expect.objectContaining({ body: expect.stringContaining('"minimumAge":null') }))
    expect(saveCall?.[1]).toEqual(expect.objectContaining({ body: expect.stringContaining('"favoriteAuthors":[]') }))
  })

  it('lets an administrator switch members and load another profile', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input)
      if (url === '/api/family/current/members') {
        return new Response(JSON.stringify([
          { memberId: 'member-1', displayName: 'Alice', role: 'Admin', preferredLanguage: 0, isActive: true, hasAccount: true },
          { memberId: 'member-2', displayName: 'Mia', role: 'Member', preferredLanguage: 1, isActive: true, hasAccount: false },
        ]), { status: 200 })
      }
      return new Response(JSON.stringify({ memberId: url.includes('member-2') ? 'member-2' : 'member-1', minimumAge: null, maximumAge: null, favoriteAuthors: [], excludedAuthors: [], favoriteGenres: [], excludedGenres: [], favoriteStyles: [], excludedStyles: [], preferredBookLanguages: [], preferenceNotes: null, profileVisibility: 'Family', useInFamilyRecommendations: true }), { status: 200 })
    })
    vi.stubGlobal('fetch', fetchMock)

    render(<RecommendationProfileSection isAdmin currentMemberId="member-1" />)

    await user.selectOptions(await screen.findByLabelText(/^member$/i), 'member-2')

    expect(fetchMock).toHaveBeenCalledWith('/api/family/current/members/member-2/recommendation-profile', { credentials: 'include' })
  })

  it('clears the previous member form while the next profile is loading', async () => {
    const user = userEvent.setup()
    let resolveSecondProfile: (response: Response) => void = () => undefined
    const secondProfile = new Promise<Response>(resolve => { resolveSecondProfile = resolve })
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input)
      if (url === '/api/family/current/members') {
        return new Response(JSON.stringify([
          { memberId: 'member-1', displayName: 'Alice', role: 'Admin', preferredLanguage: 0, isActive: true, hasAccount: true },
          { memberId: 'member-2', displayName: 'Mia', role: 'Member', preferredLanguage: 1, isActive: true, hasAccount: false },
        ]), { status: 200 })
      }
      if (url.includes('member-2')) return secondProfile
      return new Response(JSON.stringify({ memberId: 'member-1', minimumAge: null, maximumAge: null, favoriteAuthors: [], excludedAuthors: [], favoriteGenres: [], excludedGenres: [], favoriteStyles: [], excludedStyles: [], preferredBookLanguages: [], preferenceNotes: 'Alice private note', profileVisibility: 'Private', useInFamilyRecommendations: true }), { status: 200 })
    })
    vi.stubGlobal('fetch', fetchMock)

    render(<RecommendationProfileSection isAdmin currentMemberId="member-1" />)

    expect(await screen.findByDisplayValue('Alice private note')).toBeVisible()
    await user.selectOptions(screen.getByLabelText(/^member$/i), 'member-2')
    expect(screen.queryByDisplayValue('Alice private note')).not.toBeInTheDocument()

    resolveSecondProfile(new Response(JSON.stringify({ memberId: 'member-2', minimumAge: null, maximumAge: null, favoriteAuthors: [], excludedAuthors: [], favoriteGenres: [], excludedGenres: [], favoriteStyles: [], excludedStyles: [], preferredBookLanguages: [], preferenceNotes: 'Mia note', profileVisibility: 'Family', useInFamilyRecommendations: true }), { status: 200 }))
    expect(await screen.findByDisplayValue('Mia note')).toBeVisible()
  })

  it('clears members and profile data while the family member list refreshes', async () => {
    let listCallCount = 0
    let resolveRefresh: (response: Response) => void = () => undefined
    const refreshMembers = new Promise<Response>(resolve => { resolveRefresh = resolve })
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input)
      if (url === '/api/family/current/members') {
        listCallCount += 1
        if (listCallCount > 1) return refreshMembers
        return new Response(JSON.stringify([{ memberId: 'member-1', displayName: 'Alice', role: 'Admin', preferredLanguage: 0, isActive: true, hasAccount: true }]), { status: 200 })
      }
      return new Response(JSON.stringify({ memberId: 'member-1', minimumAge: null, maximumAge: null, favoriteAuthors: [], excludedAuthors: [], favoriteGenres: [], excludedGenres: [], favoriteStyles: [], excludedStyles: [], preferredBookLanguages: [], preferenceNotes: 'Old family note', profileVisibility: 'Family', useInFamilyRecommendations: true }), { status: 200 })
    })
    vi.stubGlobal('fetch', fetchMock)

    const { rerender } = render(<RecommendationProfileSection isAdmin currentMemberId="member-1" refreshKey={0} />)
    expect(await screen.findByDisplayValue('Old family note')).toBeVisible()

    rerender(<RecommendationProfileSection isAdmin currentMemberId="member-1" refreshKey={1} />)

    expect(screen.queryByDisplayValue('Old family note')).not.toBeInTheDocument()
    expect(screen.queryByText('Alice')).not.toBeInTheDocument()

    resolveRefresh(new Response(JSON.stringify([]), { status: 200 }))
    expect(await screen.findByText(/no active family members/i)).toBeVisible()
  })

  it('shows a read-only state for a forbidden profile without rendering private fields', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      if (String(input) === '/api/family/current/members') {
        return new Response(JSON.stringify([
          { memberId: 'member-1', displayName: 'Alice', role: 'Member', preferredLanguage: 0, isActive: true, hasAccount: true },
          { memberId: 'member-2', displayName: 'Mia', role: 'Member', preferredLanguage: 1, isActive: true, hasAccount: false },
        ]), { status: 200 })
      }
      if (String(input).includes('member-1')) {
        return new Response(JSON.stringify({ memberId: 'member-1', minimumAge: null, maximumAge: null, favoriteAuthors: [], excludedAuthors: [], favoriteGenres: [], excludedGenres: [], favoriteStyles: [], excludedStyles: [], preferredBookLanguages: [], preferenceNotes: null, profileVisibility: 'Family', useInFamilyRecommendations: true }), { status: 200 })
      }
      return new Response(null, { status: 403 })
    })
    vi.stubGlobal('fetch', fetchMock)

    render(<RecommendationProfileSection isAdmin={false} currentMemberId="member-1" />)

    await userEvent.setup().selectOptions(await screen.findByLabelText(/^member$/i), 'member-2')
    expect(await screen.findByRole('alert')).toHaveTextContent(/not available/i)
    expect(screen.queryByLabelText(/preference notes/i)).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /save preferences/i })).not.toBeInTheDocument()
  })

  it('keeps form values and shows an error when save fails', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      if (String(input) === '/api/family/current/members') {
        return new Response(JSON.stringify([{ memberId: 'member-1', displayName: 'Alice', role: 'Admin', preferredLanguage: 0, isActive: true, hasAccount: true }]), { status: 200 })
      }
      if (init?.method === 'PUT') return new Response(JSON.stringify({ title: 'Save failed' }), { status: 500 })
      return new Response(null, { status: 404 })
    })
    vi.stubGlobal('fetch', fetchMock)

    render(<RecommendationProfileSection isAdmin currentMemberId="member-1" />)

    const notes = await screen.findByLabelText(/preference notes/i)
    await user.type(notes, 'Keep this value.')
    await user.click(screen.getByRole('button', { name: /save preferences/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/unable to save/i)
    expect(notes).toHaveValue('Keep this value.')
  })
})
