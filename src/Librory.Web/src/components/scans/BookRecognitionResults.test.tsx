import { render, screen } from '@testing-library/react'
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

describe('BookRecognitionResults', () => {
  it('keeps provisional version confirmation visible after a purchased session is reloaded', () => {
    render(
      <BookRecognitionResults
        job={job}
        candidates={job.candidates}
        scanSessionId="scan-1"
        persistedCandidates={[persistedCandidate]}
        members={[{ memberId: 'member-1', displayName: 'Alice', role: 'Member', preferredLanguage: 0, isActive: true, hasAccount: true, hasRecommendationProfile: true, recommendationProfileVisibility: 0, canUseForFamilyRecommendations: true }]}
      />,
    )

    expect(screen.getByText('Confirm version details')).toBeVisible()
    expect(screen.getByText(/Added copy copy-1/)).toBeVisible()
  })
})
