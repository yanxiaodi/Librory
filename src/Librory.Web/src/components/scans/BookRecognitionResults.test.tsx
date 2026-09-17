import * as React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { BookRecognitionResults, shouldApplyMetadataSearchState } from './BookRecognitionResults'
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
  it('does not apply an older metadata search error to a newer request', () => {
    expect(shouldApplyMetadataSearchState(2, 1)).toBe(false)
    expect(shouldApplyMetadataSearchState(2, 2)).toBe(true)
  })

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

    expect(screen.getByText('Recognition rank: 940')).toBeVisible()
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

  it('shows persisted duplicate warnings before the purchase request', () => {
    render(
      <BookRecognitionResults
        job={pendingJob}
        candidates={pendingJob.candidates}
        scanSessionId="scan-1"
        persistedCandidates={[{
          ...pendingCandidate,
          isAlreadyOwned: true,
          duplicateMessage: 'This book is already owned by the family.',
        }]}
        members={members}
        scanTargetMemberId="member-1"
      />,
    )

    expect(screen.getByText('This book is already owned by the family.')).toBeVisible()
  })

  it('sends a manually entered author when no metadata match is selected', async () => {
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
        job={job}
        candidates={job.candidates}
        scanSessionId="scan-1"
        persistedCandidates={[pendingCandidate]}
        members={members}
        scanTargetMemberId="member-1"
      />,
    )

    await user.clear(screen.getByLabelText(/author \(optional\)/i))
    await user.type(screen.getByLabelText(/author \(optional\)/i), 'Frank Herbert')
    await user.click(screen.getByRole('button', { name: /buy this book/i }))

    const purchaseCall = fetchMock.mock.calls.find(([input]) => String(input).includes('/purchase'))
    expect(JSON.parse(purchaseCall![1]?.body as string)).toMatchObject({ manualAuthor: 'Frank Herbert' })
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

  it('merges metadata searches that complete out of order', async () => {
    const user = userEvent.setup()
    const firstSearch = deferred<Response>()
    const secondSearch = deferred<Response>()
    const twoCandidateJob = {
      ...job,
      candidates: [
        job.candidates[0],
        { ...job.candidates[0], candidateId: 'candidate-2', displayTitle: 'Matilda' },
      ],
    }
    const twoPendingCandidates = [
      pendingCandidate,
      { ...pendingCandidate, id: 'candidate-2' },
    ]
    const matildaMetadata = { ...metadata, title: 'Matilda', sourceId: 'matilda-result' }
    let searchCount = 0
    vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
      if (String(input) === '/api/book-metadata/search?title=Dune') {
        searchCount += 1
        return firstSearch.promise
      }
      if (String(input) === '/api/book-metadata/search?title=Matilda') {
        searchCount += 1
        return secondSearch.promise
      }
      throw new Error(`Unexpected fetch request: ${String(input)}`)
    }))

    function Harness() {
      const [candidates, setCandidates] = React.useState(twoCandidateJob.candidates)
      return (
        <BookRecognitionResults
          job={twoCandidateJob}
          candidates={candidates}
          onCandidatesChange={setCandidates}
          scanSessionId="scan-1"
          persistedCandidates={twoPendingCandidates}
          members={members}
          scanTargetMemberId="member-1"
        />
      )
    }

    render(<Harness />)
    const searchInputs = screen.getAllByLabelText(/search text/i)
    const searchButtons = screen.getAllByRole('button', { name: /re-search metadata/i })
    expect(searchButtons).toHaveLength(2)
    await user.click(searchButtons[0])
    await user.click(searchButtons[1])
    expect((vi.mocked(fetch).mock.calls as Array<[RequestInfo | URL]>).map(([input]) => String(input))).toEqual([
      '/api/book-metadata/search?title=Dune',
      '/api/book-metadata/search?title=Matilda',
    ])

    secondSearch.resolve(new Response(JSON.stringify({ candidates: [matildaMetadata] }), { status: 200 }))
    await waitFor(() => expect(screen.getByRole('option', { name: /Matilda · Frank Herbert/ })).toBeVisible())
    firstSearch.resolve(new Response(JSON.stringify({ candidates: [{ ...metadata, sourceId: 'dune-result' }] }), { status: 200 }))

    expect(searchInputs).toHaveLength(2)
    await waitFor(() => expect(screen.getByRole('option', { name: /Dune · Frank Herbert/ })).toBeVisible())
    expect(screen.getByRole('option', { name: /Matilda · Frank Herbert/ })).toBeVisible()
    expect(searchCount).toBe(2)
  })

  it('keeps metadata correction retry local to the candidate', async () => {
    const user = userEvent.setup()
    let saveAttempts = 0
    const onMetadataMatchesChange = vi.fn(async () => {
      saveAttempts += 1
      if (saveAttempts === 1) throw new Error('Metadata matches were found, but the corrected candidate could not be saved.')
    })
    vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
      if (String(input) === '/api/book-metadata/search?title=Dune') {
        return new Response(JSON.stringify({ candidates: [metadata] }), { status: 200 })
      }
      throw new Error(`Unexpected fetch request: ${String(input)}`)
    }))

    render(
      <BookRecognitionResults
        job={pendingJob}
        candidates={pendingJob.candidates}
        scanSessionId="scan-1"
        persistedCandidates={[pendingCandidate]}
        members={members}
        scanTargetMemberId="member-1"
        onMetadataMatchesChange={onMetadataMatchesChange}
      />,
    )

    const searchButton = screen.getByRole('button', { name: /re-search metadata/i })
    await user.click(searchButton)
    expect(await screen.findByText(/could not be saved/i)).toBeVisible()

    await user.click(screen.getByRole('button', { name: /re-search metadata/i }))
    await waitFor(() => expect(onMetadataMatchesChange).toHaveBeenCalledTimes(2))
    expect(screen.queryByText(/could not be saved/i)).not.toBeInTheDocument()
  })
})

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason?: unknown) => void
  const promise = new Promise<T>((resolvePromise, rejectPromise) => {
    resolve = resolvePromise
    reject = rejectPromise
  })
  return { promise, resolve, reject }
}
