import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ScansPage } from './ScansPage'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('ScansPage', () => {
  it('uploads a shelf photo and switches to scanning state', async () => {
    const user = userEvent.setup()
    let resolveFetch: ((response: Response) => void) | undefined

    const fetchMock = vi.fn(
      () =>
        new Promise<Response>(resolve => {
          resolveFetch = resolve
        }),
    )

    vi.stubGlobal('fetch', fetchMock)

    render(<ScansPage />)

    expect(screen.getByRole('button', { name: /scan a shelf/i })).toBeVisible()

    const file = new File(['fake image'], 'shelf.jpg', { type: 'image/jpeg' })
    await user.upload(screen.getByLabelText(/shelf photo/i), file)

    expect(await screen.findByText(/uploading photo/i)).toBeVisible()
    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/family/current/scan-sessions/uploads',
      expect.objectContaining({
        method: 'POST',
        credentials: 'include',
        body: expect.any(FormData),
      }),
    )

    const requestInit = fetchMock.mock.calls[0][1] as RequestInit
    const formData = requestInit.body as FormData
    expect(formData.get('photo')).toBe(file)

    resolveFetch?.(
      new Response(
        JSON.stringify({
          scanSessionId: 'scan-1',
          familyId: 'family-1',
          shelfPhotoPath: '/tmp/Librory/scan-uploads/shelf.jpg',
          candidates: [],
          expiresAt: '2026-07-28T00:00:00Z',
        }),
        {
          status: 201,
          headers: {
            'Content-Type': 'application/json',
          },
        },
      ),
    )

    expect(await screen.findByText(/scanning in progress/i)).toBeVisible()
    expect(screen.getByText(/scan session:/i)).toBeVisible()
    expect(screen.getByText(/scan-1/i)).toBeVisible()
  })

  it('shows an error when the upload fails', async () => {
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
})
