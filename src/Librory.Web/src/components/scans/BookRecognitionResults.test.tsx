import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { BookRecognitionResults } from './BookRecognitionResults'
import type { BookRecognitionJobResponse } from '@/lib/bookRecognitionApi'
import type { ScanCandidateResponse } from '@/lib/scansApi'

const job: BookRecognitionJobResponse = {
  jobId: 'job-1',
  familyId: 'family-1',
  status: 2,
  sourcePhotoPath: 'shelf.jpg',
  candidates: [{
    candidateId: 'candidate-1',
    displayTitle: 'Dune',
    evidenceText: 'DUNE',
    rank: 940,
    metadataMatches: [],
  }],
  warnings: [],
  failureMessage: null,
  createdAt: '2026-09-15T00:00:00Z',
  updatedAt: '2026-09-15T00:00:00Z',
}

const purchase = {
  copy: {
    bookCopyId: 'copy-1',
    familyId: 'family-1',
    memberId: 'member-1',
    bookEditionId: 'edition-1',
    duplicateStatus: 1 as const,
    condition: null,
    purchaseStore: null,
    purchasePrice: null,
    shelfLocation: null,
    purchasedAt: '2026-09-15T00:00:00Z',
    intakeNotes: null,
    purchasedByMemberId: 'member-1',
  },
      work: {
    bookWorkId: 'work-1',
    title: 'Dune',
    author: 'Frank Herbert',
    editions: [{
      bookEditionId: 'edition-1',
      isbn: null,
      format: null,
      publicationYear: null,
      isProvisional: true,
    }],
      },
      bookEditionId: 'edition-1',
      duplicateStatus: 1 as const,
      isProvisional: true,
}

const persistedCandidate: ScanCandidateResponse = {
  id: 'candidate-1',
  displayTitle: 'Dune',
  author: 'Frank Herbert',
  recommendationScore: null,
  isAlreadyOwned: false,
  duplicateMessage: null,
  confidenceLabel: 'DUNE',
  detectedLanguage: null,
  recognitionRank: 1,
  metadataSnapshot: null,
  purchaseStatus: 1,
  purchasedBookCopyId: 'copy-1',
  purchaseRequestId: 'request-1',
  purchasedAt: '2026-09-15T00:00:00Z',
  purchase,
}

const metadata = {
  source: 'GoogleBooks',
  sourceId: 'volume-1',
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
}

const pendingJob: BookRecognitionJobResponse = {
  ...job,
  candidates: [{ ...job.candidates[0], metadataMatches: [metadata] }],
}

const pendingCandidate: ScanCandidateResponse = {
  ...persistedCandidate,
  purchaseStatus: 0,
  purchasedBookCopyId: null,
  purchaseRequestId: null,
  purchasedAt: null,
  purchase: null,
}

const members = [{ memberId: 'member-1', displayName: 'Alice', role: 'Member', preferredLanguage: 0, isActive: true, hasAccount: true, hasRecommendationProfile: true, recommendationProfileVisibility: 0, canUseForFamilyRecommendations: true }]

describe('BookRecognitionResults', () => {
  it('keeps provisional version confirmation visible after a purchased session is reloaded', () => {
    render(
      <BookRecognitionResults
        job={job}
        candidates={job.candidates}
        scanSessionId="scan-1"
        persistedCandidates={[persistedCandidate]}
        members={members}
      />,
    )

    expect(screen.getByText('Confirm version details')).toBeVisible()
    expect(screen.getByText(/Added copy copy-1/)).toBeVisible()
  })

  it('sends optional intake fields when buying a pending candidate', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn(async (input: RequestInfo | URL, _init?: RequestInit) => {
      if (String(input).includes('/purchase')) {
        return new Response(JSON.stringify({ ...purchase, isReplay: false }), { status: 201 })
      }
      throw new Error(`Unexpected fetch request: ${String(input)}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(
      <BookRecognitionResults
        job={pendingJob}
        candidates={pendingJob.candidates}
        scanSessionId="scan-1"
        persistedCandidates={[pendingCandidate]}
        members={members}
        scanTargetMemberId="member-1"
      />,
    )

    await user.type(screen.getByLabelText(/store \(optional\)/i), 'Unity Books')
    await user.type(screen.getByLabelText(/condition \(optional\)/i), 'New')
    await user.type(screen.getByLabelText(/price \(optional\)/i), '24.50')
    await user.type(screen.getByLabelText(/shelf location \(optional\)/i), 'A-3')
    await user.type(screen.getByLabelText(/intake notes \(optional\)/i), 'Gift')
    await user.click(screen.getByRole('button', { name: /buy this book/i }))

    const purchaseCall = fetchMock.mock.calls.find(([input]) => String(input).includes('/purchase'))
    expect(purchaseCall).toBeDefined()
    expect(JSON.parse(purchaseCall![1]?.body as string)).toMatchObject({
      purchaseStore: 'Unity Books',
      condition: 'New',
      purchasePrice: 24.5,
      shelfLocation: 'A-3',
      intakeNotes: 'Gift',
    })
  })

  it('requires a fresh metadata search after the title is edited', async () => {
    const user = userEvent.setup()

    render(
      <BookRecognitionResults
        job={pendingJob}
        candidates={pendingJob.candidates}
        scanSessionId="scan-1"
        persistedCandidates={[pendingCandidate]}
        members={members}
        scanTargetMemberId="member-1"
      />,
    )

    const searchText = screen.getByLabelText(/search text/i)
    await user.clear(searchText)
    await user.type(searchText, 'Dune Messiah')

    expect(screen.getByRole('button', { name: /buy this book/i })).toBeDisabled()
  })

  it('submits version confirmation for a reloaded provisional purchase', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      if (String(input).includes('/book-editions/edition-1/version')) {
        return new Response(JSON.stringify({
          bookEditionId: 'edition-1',
          isbn: '9780441013593',
          format: 'Paperback',
          publicationYear: 1965,
          isProvisional: false,
        }), { status: 200 })
      }
      throw new Error(`Unexpected fetch request: ${String(input)}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(
      <BookRecognitionResults
        job={job}
        candidates={job.candidates}
        scanSessionId="scan-1"
        persistedCandidates={[persistedCandidate]}
        members={members}
        scanTargetMemberId="member-1"
      />,
    )

    await user.type(screen.getByLabelText(/ISBN for Dune/i), '9780441013593')
    await user.click(screen.getByRole('button', { name: /confirm version/i }))

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/family/current/book-editions/edition-1/version',
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify({ isbn: '9780441013593' }),
      }),
    )
  })
})
