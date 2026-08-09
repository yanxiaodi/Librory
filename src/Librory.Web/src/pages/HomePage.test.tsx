import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { vi } from 'vitest'
import App from '@/App'
import { AuthSessionProvider } from '@/auth/AuthSessionContext'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('HomePage', () => {
  it('shows the latest scan session when one exists', async () => {
    vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
      if (String(input) === '/api/family/current/scan-sessions/latest') {
        return new Response(JSON.stringify({
          scanSessionId: 'scan-1',
          familyId: 'family-1',
          shelfPhotoPath: '/tmp/Librory/scan-uploads/shelf.jpg',
          candidates: [{ candidateId: 'candidate-1' }],
          expiresAt: '2026-08-08T00:00:00Z',
          targetMemberId: 'member-1',
          targetMemberDisplayName: 'Alice',
          targetProfileAvailable: true,
          targetProfileUsed: true,
          inferredLanguage: 0,
          hasMixedLanguages: false,
        }), { status: 200 })
      }

      throw new Error(`Unexpected fetch request: ${String(input)}`)
    }))

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

    expect(await screen.findByText(/scan prepared for alice/i)).toBeVisible()
    expect(screen.getByText(/1 candidate saved/i)).toBeVisible()
    expect(screen.getByRole('link', { name: /scan a shelf/i })).toBeVisible()
    expect(screen.getByRole('link', { name: /browse library/i })).toBeVisible()
    expect(screen.getByText(/^recent scans$/i)).toBeVisible()
    expect(screen.getByText(/your family's reading companion/i)).toBeVisible()
  })

  it('falls back to the empty state when no scan session exists', async () => {
    vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
      if (String(input) === '/api/family/current/scan-sessions/latest') {
        return new Response('', { status: 404 })
      }

      throw new Error(`Unexpected fetch request: ${String(input)}`)
    }))

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

    expect(await screen.findByText(/start with your first shelf scan/i)).toBeVisible()
  })
})
