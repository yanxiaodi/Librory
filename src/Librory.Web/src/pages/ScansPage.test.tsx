import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ScansPage } from './ScansPage'

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
})

describe('ScansPage', () => {
  it('uploads a shelf photo and renders recognized candidates after polling', async () => {
    const user = userEvent.setup()
    let resolvePoll: ((response: Response) => void) | undefined

    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input)

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

      throw new Error(`Unexpected fetch request: ${url}`)
    })

    vi.stubGlobal('fetch', fetchMock)

    render(<ScansPage />)

    expect(screen.getByRole('button', { name: /scan a shelf/i })).toBeVisible()

    const file = new File(['fake image'], 'shelf.jpg', { type: 'image/jpeg' })
    await user.upload(screen.getByLabelText(/shelf photo/i), file)

    expect(fetchMock.mock.calls.length).toBeGreaterThanOrEqual(1)
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/book-recognition-jobs',
      expect.objectContaining({
        method: 'POST',
        credentials: 'include',
        body: expect.any(FormData),
      }),
    )

    const requestInit = fetchMock.mock.calls[0][1] as RequestInit
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

  it('shows an error when the recognition upload fails', async () => {
    const user = userEvent.setup()

    vi.stubGlobal(
      'fetch',
      vi.fn(async () => {
        return new Response('nope', { status: 500 })
      }),
    )

    render(<ScansPage />)

    expect(screen.getByRole('button', { name: /scan a shelf/i })).toBeVisible()

    await user.upload(screen.getByLabelText(/shelf photo/i), new File(['fake'], 'shelf.jpg', { type: 'image/jpeg' }))

    expect(await screen.findByText(/^upload failed$/i)).toBeVisible()
    expect(screen.getByText(/try the shelf photo again/i)).toBeVisible()
  })

  it('does not reopen the picker while uploading', async () => {
    const user = userEvent.setup()
    let resolvePost: ((response: Response) => void) | undefined

    vi.stubGlobal(
      'fetch',
      vi.fn(
        () =>
          new Promise<Response>(resolve => {
            resolvePost = resolve
          }),
      ),
    )

    render(<ScansPage />)

    const file = new File(['fake image'], 'shelf.jpg', { type: 'image/jpeg' })
    await user.upload(screen.getByLabelText(/shelf photo/i), file)

    expect(await screen.findByText(/uploading photo/i)).toBeVisible()

    const button = screen.getByRole('button', { name: /uploading/i })
    button.focus()
    await user.keyboard('{Enter}')

    expect(fetch).toHaveBeenCalledTimes(1)

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
})
