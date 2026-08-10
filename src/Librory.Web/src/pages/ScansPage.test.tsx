import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthSessionProvider } from '@/auth/AuthSessionContext'
import { PENDING_JOB_STORAGE_KEY, ScansPage } from './ScansPage'

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
  sessionStorage.clear()
})

describe('ScansPage', () => {
  it('defaults the scan target to the current member and allows an eligible member', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      if (String(input) === '/api/family/current/members') {
        return new Response(JSON.stringify([
          { memberId: 'member-1', displayName: 'Alice', role: 'Admin', preferredLanguage: 0, isActive: false, hasAccount: true, canUseForFamilyRecommendations: false },
          { memberId: 'member-2', displayName: 'Bob', role: 'Member', preferredLanguage: 0, isActive: true, hasAccount: false, canUseForFamilyRecommendations: true },
          { memberId: 'member-3', displayName: 'Inactive', role: 'Member', preferredLanguage: 0, isActive: false, hasAccount: false, canUseForFamilyRecommendations: true },
        ]), { status: 200 })
      }

      throw new Error(`Unexpected fetch request: ${String(input)}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(
      <AuthSessionProvider initialSession={{
        status: 'authenticated',
        user: { id: 'member-1', displayName: 'Alice', role: 'Admin' },
        family: { id: 'family-1', name: 'The Yans', memberId: 'member-1', memberCount: 2 },
      }}>
        <ScansPage />
      </AuthSessionProvider>,
    )

    const target = await screen.findByLabelText(/scan for member/i)
    expect(target).toHaveValue('member-1')
    expect(screen.getByRole('option', { name: 'Alice' })).toBeVisible()
    expect(screen.getByRole('option', { name: 'Bob' })).toBeVisible()
    expect(screen.queryByRole('option', { name: 'Inactive' })).not.toBeInTheDocument()

    await user.selectOptions(target, 'member-2')
    expect(target).toHaveValue('member-2')
  })

  it('persists the selected target and renders the returned recommendation context', async () => {
    const user = userEvent.setup()
    let sessionPayload: Record<string, unknown> | undefined
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input)
      if (url === '/api/family/current/members') {
        return new Response(JSON.stringify([
          { memberId: 'member-1', displayName: 'Alice', role: 'Admin', preferredLanguage: 0, isActive: true, hasAccount: true, canUseForFamilyRecommendations: true },
          { memberId: 'member-2', displayName: 'Bob', role: 'Member', preferredLanguage: 0, isActive: true, hasAccount: false, canUseForFamilyRecommendations: true },
        ]), { status: 200 })
      }
      if (url === '/api/book-recognition-jobs' && init?.method === 'POST') {
        return new Response(JSON.stringify({
          jobId: 'job-2', familyId: 'family-1', status: 2,
          sourcePhotoPath: '/tmp/Librory/scan-uploads/shelf.jpg', candidates: [{
            candidateId: 'candidate-1', displayTitle: 'Dune', evidenceText: 'DUNE', rank: 940,
            metadataMatches: [{ source: 'google-books', sourceId: 'source-1', title: 'Dune', subtitle: null, authors: ['Frank Herbert'], publisher: null, publishedDate: null, language: 'en', description: null, isbn10: null, isbn13: null, thumbnailUrl: null, infoUrl: null }],
          }], warnings: [], failureMessage: null, createdAt: '2026-08-07T00:00:00Z', updatedAt: '2026-08-07T00:00:00Z',
        }), { status: 202 })
      }
      if (url === '/api/family/current/scan-sessions' && init?.method === 'POST') {
        sessionPayload = JSON.parse(init.body as string) as Record<string, unknown>
        return new Response(JSON.stringify({
          scanSessionId: 'scan-2', familyId: 'family-1', shelfPhotoPath: '/tmp/Librory/scan-uploads/shelf.jpg', candidates: [], expiresAt: '2026-08-08T00:00:00Z',
          targetMemberId: 'member-2', targetMemberDisplayName: 'Bob', targetProfileAvailable: true, targetProfileUsed: true, inferredLanguage: 0, hasMixedLanguages: false,
        }), { status: 201 })
      }
      throw new Error(`Unexpected fetch request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(
      <AuthSessionProvider initialSession={{
        status: 'authenticated', user: { id: 'member-1', displayName: 'Alice', role: 'Admin' }, family: { id: 'family-1', name: 'The Yans', memberId: 'member-1', memberCount: 2 },
      }}>
        <ScansPage />
      </AuthSessionProvider>,
    )

    await user.selectOptions(await screen.findByLabelText(/scan for member/i), 'member-2')
    await user.upload(screen.getByLabelText(/shelf photo/i), new File(['fake image'], 'shelf.jpg', { type: 'image/jpeg' }))

    expect(await screen.findByText(/recommendation context/i)).toBeVisible()
    expect(screen.getByText(/scan prepared for bob/i)).toBeVisible()
    expect(screen.getByText(/profile: used/i)).toBeVisible()
    expect(screen.getByText(/language context: english/i)).toBeVisible()
    expect(sessionPayload).toMatchObject({
      shelfPhotoPath: '/tmp/Librory/scan-uploads/shelf.jpg',
    })
    expect(sessionPayload).toMatchObject({
      targetMemberId: 'member-2',
      candidates: [{ displayTitle: 'Dune', confidenceLabel: 'DUNE', author: 'Frank Herbert', detectedLanguage: 0 }],
    })
  })

  it('uploads a shelf photo and renders recognized candidates after polling', async () => {
    const user = userEvent.setup()
    let resolvePoll: ((response: Response) => void) | undefined

    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input)

      if (url === '/api/family/current/members') {
        return new Response(JSON.stringify([]), { status: 200 })
      }

      if (url === '/api/book-recognition-jobs' && init?.method === 'POST') {
        return new Response(
          JSON.stringify({
            jobId: 'job-1',
            familyId: 'family-1',
            status: 0,
            sourcePhotoPath: '/tmp/Librory/scan-uploads/shelf.jpg',
            candidates: [],
            warnings: [],
            failureMessage: null,
            createdAt: '2026-08-03T00:00:00Z',
            updatedAt: '2026-08-03T00:00:00Z',
          }),
          {
            status: 202,
            headers: {
              'Content-Type': 'application/json',
            },
          },
        )
      }

      if (url === '/api/book-recognition-jobs/job-1' && init?.credentials === 'include') {
        return new Promise<Response>(resolve => {
          resolvePoll = resolve
        })
      }

      if (url === '/api/family/current/scan-sessions' && init?.method === 'POST') {
        return new Response(JSON.stringify({
          scanSessionId: 'scan-1',
          familyId: 'family-1',
          shelfPhotoPath: '/tmp/Librory/scan-uploads/shelf.jpg',
          candidates: [],
          expiresAt: '2026-08-08T00:00:00Z',
          targetMemberId: null,
          targetMemberDisplayName: 'Current member',
          targetProfileAvailable: false,
          targetProfileUsed: false,
          inferredLanguage: 0,
          hasMixedLanguages: false,
        }), { status: 201 })
      }

      throw new Error(`Unexpected fetch request: ${url}`)
    })

    vi.stubGlobal('fetch', fetchMock)

    render(<ScansPage />)

    expect(screen.getByRole('button', { name: /scan a shelf/i })).toBeVisible()

    const file = new File(['fake image'], 'shelf.jpg', { type: 'image/jpeg' })
    await user.upload(screen.getByLabelText(/shelf photo/i), file)

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/book-recognition-jobs',
      expect.objectContaining({
        method: 'POST',
        credentials: 'include',
        body: expect.any(FormData),
      }),
    )

    const uploadCall = fetchMock.mock.calls.find(([input]) => String(input) === '/api/book-recognition-jobs')
    if (!uploadCall) {
      throw new Error('Expected a book-recognition upload request.')
    }
    const requestInit = uploadCall[1] as RequestInit
    const formData = requestInit.body as FormData
    expect(formData.get('photo')).toBe(file)

    expect(await screen.findByText(/polling recognition job/i)).toBeVisible()

    resolvePoll?.(
      new Response(
        JSON.stringify({
          jobId: 'job-1',
          familyId: 'family-1',
          status: 2,
          sourcePhotoPath: '/tmp/Librory/scan-uploads/shelf.jpg',
          candidates: [
            {
              candidateId: 'candidate-1',
              displayTitle: 'Dune',
              evidenceText: 'DUNE',
              rank: 940,
              metadataMatches: [
                {
                  source: 'google-books',
                  sourceId: 'source-1',
                  title: 'Dune',
                  subtitle: null,
                  authors: ['Frank Herbert'],
                  publisher: 'Ace',
                  publishedDate: '1965',
                  language: 'en',
                  description: null,
                  isbn10: '0441013597',
                  isbn13: '9780441013593',
                  thumbnailUrl: null,
                  infoUrl: null,
                },
              ],
            },
          ],
          warnings: [],
          failureMessage: null,
          createdAt: '2026-08-03T00:00:00Z',
          updatedAt: '2026-08-03T00:00:05Z',
        }),
        {
          status: 200,
          headers: {
            'Content-Type': 'application/json',
          },
        },
      ),
    )

    expect(await screen.findByText(/recognition complete/i)).toBeVisible()
    expect(screen.getByRole('heading', { name: 'Dune' })).toBeVisible()
    expect(screen.getByText(/Frank Herbert/i, { selector: 'li' })).toBeVisible()
  })

  it('lets the user remove a candidate and edit its search text', async () => {
    const user = userEvent.setup()

    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input)

        if (url === '/api/family/current/members') {
          return new Response(JSON.stringify([]), { status: 200 })
        }

        if (url === '/api/book-recognition-jobs' && init?.method === 'POST') {
          return new Response(JSON.stringify({
            jobId: 'job-1',
            familyId: 'family-1',
            status: 2,
            sourcePhotoPath: '/tmp/Librory/scan-uploads/shelf.jpg',
            candidates: [
              {
                candidateId: 'candidate-1',
                displayTitle: 'Dune',
                evidenceText: 'DUNE',
                rank: 940,
                metadataMatches: [],
              },
            ],
            warnings: [],
            failureMessage: null,
            createdAt: '2026-08-03T00:00:00Z',
            updatedAt: '2026-08-03T00:00:00Z',
          }), { status: 202 })
        }

        throw new Error(`Unexpected fetch request: ${url}`)
      }),
    )

    render(<ScansPage />)

    await user.upload(screen.getByLabelText(/shelf photo/i), new File(['fake image'], 'shelf.jpg', { type: 'image/jpeg' }))

    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeVisible()

    const searchText = screen.getByLabelText(/search text/i)
    await user.clear(searchText)
    await user.type(searchText, 'Dune Messiah')
    expect(searchText).toHaveValue('Dune Messiah')

    await user.click(screen.getByRole('button', { name: /remove/i }))
    expect(screen.queryByRole('heading', { name: 'Dune' })).not.toBeInTheDocument()
    expect(screen.getByText(/no candidates were found yet/i)).toBeVisible()
  })

  it('renders the persisted scan session context after recognition completes', async () => {
    const user = userEvent.setup()

    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input)

        if (url === '/api/family/current/members') {
          return new Response(JSON.stringify([]), { status: 200 })
        }

        if (url === '/api/book-recognition-jobs' && init?.method === 'POST') {
          return new Response(JSON.stringify({
            jobId: 'job-1',
            familyId: 'family-1',
            status: 2,
            sourcePhotoPath: '/tmp/Librory/scan-uploads/shelf.jpg',
            candidates: [
              {
                candidateId: 'candidate-1',
                displayTitle: 'Dune',
                evidenceText: 'DUNE',
                rank: 940,
                metadataMatches: [],
              },
            ],
            warnings: [],
            failureMessage: null,
            createdAt: '2026-08-03T00:00:00Z',
            updatedAt: '2026-08-03T00:00:00Z',
          }), { status: 202 })
        }

        if (url === '/api/family/current/scan-sessions' && init?.method === 'POST') {
          return new Response(JSON.stringify({
            scanSessionId: 'scan-1',
            familyId: 'family-1',
            shelfPhotoPath: '/tmp/Librory/scan-uploads/shelf.jpg',
            candidates: [],
            expiresAt: '2026-08-08T00:00:00Z',
            targetMemberId: null,
            targetMemberDisplayName: 'Current member',
            targetProfileAvailable: false,
            targetProfileUsed: false,
            inferredLanguage: 0,
            hasMixedLanguages: false,
          }), { status: 201 })
        }

        throw new Error(`Unexpected fetch request: ${url}`)
      }),
    )

    render(<ScansPage />)

    await user.upload(screen.getByLabelText(/shelf photo/i), new File(['fake image'], 'shelf.jpg', { type: 'image/jpeg' }))

    expect(await screen.findByText(/recommendation context/i)).toBeVisible()
    expect(screen.getByText(/scan prepared for current member/i)).toBeVisible()
    expect(screen.getByText(/profile: not available/i)).toBeVisible()
    expect(screen.getByText(/language context: english/i)).toBeVisible()
  })

  it('shows an error when the recognition upload fails', async () => {
    const user = userEvent.setup()

    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL) => {
        if (String(input) === '/api/family/current/members') return new Response(JSON.stringify([]), { status: 200 })
        return new Response('nope', { status: 500 })
      }),
    )

    render(<ScansPage />)

    expect(screen.getByRole('button', { name: /scan a shelf/i })).toBeVisible()

    await user.upload(screen.getByLabelText(/shelf photo/i), new File(['fake'], 'shelf.jpg', { type: 'image/jpeg' }))

    expect(await screen.findByText(/^recognition failed$/i)).toBeVisible()
    expect(screen.getByText(/the recognition job did not complete/i)).toBeVisible()
  })

  it('does not reopen the picker while uploading', async () => {
    const user = userEvent.setup()
    let resolvePost: ((response: Response) => void) | undefined

    vi.stubGlobal(
      'fetch',
      vi.fn(
        (input: RequestInfo | URL) => {
          if (String(input) === '/api/family/current/members') return Promise.resolve(new Response(JSON.stringify([]), { status: 200 }))
          return new Promise<Response>(resolve => {
            resolvePost = resolve
          })
        },
      ),
    )

    render(<ScansPage />)

    const file = new File(['fake image'], 'shelf.jpg', { type: 'image/jpeg' })
    await user.upload(screen.getByLabelText(/shelf photo/i), file)

    expect(await screen.findByText(/uploading photo/i)).toBeVisible()

    const button = screen.getByRole('button', { name: /uploading/i })
    button.focus()
    await user.keyboard('{Enter}')

    expect(fetch).toHaveBeenCalledTimes(2)

    resolvePost?.(
      new Response(
        JSON.stringify({
          jobId: 'job-1',
          familyId: 'family-1',
          status: 0,
          sourcePhotoPath: '/tmp/Librory/scan-uploads/shelf.jpg',
          candidates: [],
          warnings: [],
          failureMessage: null,
          createdAt: '2026-08-03T00:00:00Z',
          updatedAt: '2026-08-03T00:00:00Z',
        }),
        {
          status: 202,
          headers: {
            'Content-Type': 'application/json',
          },
        },
      ),
    )
  })

  it('resumes a pending recognition job from sessionStorage and clears it once the job completes', async () => {
    sessionStorage.setItem(PENDING_JOB_STORAGE_KEY, JSON.stringify({ jobId: 'job-resumed', targetMemberId: 'member-1' }))

    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input)

      if (url === '/api/family/current/members') {
        return new Response(JSON.stringify([]), { status: 200 })
      }

      if (url === '/api/book-recognition-jobs/job-resumed') {
        return new Response(JSON.stringify({
          jobId: 'job-resumed',
          familyId: 'family-1',
          status: 3,
          sourcePhotoPath: '/tmp/Librory/scan-uploads/shelf.jpg',
          candidates: [],
          warnings: [],
          failureMessage: 'Recognition failed.',
          createdAt: '2026-08-03T00:00:00Z',
          updatedAt: '2026-08-03T00:00:05Z',
        }), { status: 200 })
      }

      throw new Error(`Unexpected fetch request: ${url}`)
    })

    vi.stubGlobal('fetch', fetchMock)

    render(<ScansPage />)

    expect(await screen.findByText(/the recognition job did not complete/i)).toBeVisible()
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/book-recognition-jobs/job-resumed',
      expect.objectContaining({ credentials: 'include' }),
    )
    expect(sessionStorage.getItem(PENDING_JOB_STORAGE_KEY)).toBeNull()
  })
})
