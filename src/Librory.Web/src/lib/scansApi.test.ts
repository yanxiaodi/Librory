import { afterEach, describe, expect, it, vi } from 'vitest'
import { createScanSession, getLatestScanSession, type CreateScanSessionRequest } from './scansApi'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('scansApi', () => {
  it('creates a scan session with its target and candidates', async () => {
    const input: CreateScanSessionRequest = {
      shelfPhotoPath: '/tmp/Librory/scan-uploads/shelf.jpg',
      targetMemberId: 'member-2',
      candidates: [{
        displayTitle: 'Dune',
        confidenceLabel: 'DUNE',
        author: 'Frank Herbert',
        recommendationScore: 0.94,
        detectedLanguage: 0,
      }],
    }
    const responseBody = {
      scanSessionId: 'scan-1',
      familyId: 'family-1',
      shelfPhotoPath: input.shelfPhotoPath,
      candidates: [],
      expiresAt: '2026-08-08T00:00:00Z',
      targetMemberId: 'member-2',
      targetMemberDisplayName: 'Bob',
      targetProfileAvailable: true,
      targetProfileUsed: true,
      inferredLanguage: 0,
      hasMixedLanguages: false,
    }
    const fetchMock = vi.fn(async (_input: RequestInfo | URL, _init?: RequestInit) => new Response(JSON.stringify(responseBody), { status: 201 }))
    vi.stubGlobal('fetch', fetchMock)

    await expect(createScanSession(input)).resolves.toEqual(responseBody)

    expect(fetchMock).toHaveBeenCalledWith('/api/family/current/scan-sessions', expect.objectContaining({
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
    }))
    expect(JSON.parse((fetchMock.mock.calls[0][1] as RequestInit).body as string)).toEqual(input)
  })

  it('throws when scan session creation fails', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response('nope', { status: 400 })))

    await expect(createScanSession({ shelfPhotoPath: '/tmp/shelf.jpg' })).rejects.toThrow('Scan session creation failed (400).')
  })

  it('returns null when no latest scan session exists', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response('', { status: 404 })))

    await expect(getLatestScanSession()).resolves.toBeNull()
  })
})
