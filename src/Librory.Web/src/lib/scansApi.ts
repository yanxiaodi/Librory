import type { BookMetadataCandidateResponse } from './bookRecognitionApi'

export type PurchaseStatus = 0 | 1

export type ScanCandidateMetadataSnapshot = {
  schemaVersion: number
  evidenceText: string | null
  matches: BookMetadataCandidateResponse[]
}

export type ScanCandidateResponse = {
  id: string
  displayTitle: string
  author: string | null
  recommendationScore: number | null
  isAlreadyOwned: boolean
  duplicateMessage: string | null
  confidenceLabel: string
  detectedLanguage: number | null
  recognitionRank: number
  metadataSnapshot: ScanCandidateMetadataSnapshot | null
  purchaseStatus: PurchaseStatus
  purchasedBookCopyId: string | null
  purchaseRequestId: string | null
  purchasedAt: string | null
  purchase: Omit<ScanPurchaseResponse, 'isReplay'> | null
}

export interface ScanSessionResponse {
  scanSessionId: string
  familyId: string
  shelfPhotoPath: string
  candidates: ScanCandidateResponse[]
  expiresAt: string
  targetMemberId: string | null
  targetMemberDisplayName: string
  targetProfileAvailable: boolean
  targetProfileUsed: boolean
  inferredLanguage: number | null
  hasMixedLanguages: boolean
}

export interface CreateScanCandidateRequest {
  displayTitle: string
  confidenceLabel: string
  author?: string
  recommendationScore?: number | null
  isAlreadyOwned?: boolean
  duplicateMessage?: string
  detectedLanguage?: number
  recognitionEvidence?: string
  recognitionRank?: number
  metadataMatches?: BookMetadataCandidateResponse[]
}

export type DuplicateResolution = 0 | 1 | 2 | 3

export type ConfirmScanPurchaseRequest = {
  purchaseRequestId: string
  ownerMemberId: string
  selectedMetadata?: BookMetadataCandidateResponse
  duplicateResolution?: DuplicateResolution
  duplicateStatus?: 0 | 1 | 2
  existingBookEditionId?: string
  existingBookWorkId?: string
  manualTitle?: string
  manualAuthor?: string
  isbn?: string
  format?: string
  publicationYear?: number
  condition?: string
  purchaseStore?: string
  purchasePrice?: number
  shelfLocation?: string
  purchasedAt?: string
  intakeNotes?: string
}

export type ScanPurchaseResponse = {
  copy: {
    bookCopyId: string
    familyId: string
    memberId: string
    bookEditionId: string
    duplicateStatus: 0 | 1 | 2
    condition: string | null
    purchaseStore: string | null
    purchasePrice: number | null
    shelfLocation: string | null
    purchasedAt: string | null
    intakeNotes: string | null
    purchasedByMemberId: string | null
  }
  work: { bookWorkId: string; title: string; author: string | null; editions: Array<{ bookEditionId: string; isbn: string | null; format: string | null; publicationYear: number | null; isProvisional: boolean }> }
  bookEditionId: string
  isProvisional: boolean
  duplicateStatus: 0 | 1 | 2
  isReplay: boolean
}

export type BookEditionVersionResponse = {
  bookEditionId: string
  isbn: string | null
  format: string | null
  publicationYear: number | null
  isProvisional: boolean
}

export type DuplicateConfirmationResponse = {
  message: string
  followUpHint: string | null
  matches: Array<{ bookCopyId: string; bookEditionId: string; bookWorkId: string; title: string; isbn: string | null; format: string | null; publicationYear: number | null }>
}

export class ScanPurchaseError extends Error {
  constructor(public readonly status: number, message: string, public readonly duplicate?: DuplicateConfirmationResponse) {
    super(message)
    this.name = 'ScanPurchaseError'
  }
}

export interface CreateScanSessionRequest {
  shelfPhotoPath: string
  retentionWindowDays?: number
  candidates?: CreateScanCandidateRequest[]
  targetMemberId?: string
}

export async function createScanSession(input: CreateScanSessionRequest): Promise<ScanSessionResponse> {
  const response = await fetch('/api/family/current/scan-sessions', {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })

  if (!response.ok) {
    throw new Error(`Scan session creation failed (${response.status}).`)
  }

  return response.json() as Promise<ScanSessionResponse>
}

export async function uploadShelfPhoto(file: File): Promise<ScanSessionResponse> {
  const formData = new FormData()
  formData.append('photo', file)

  const response = await fetch('/api/family/current/scan-sessions/uploads', {
    method: 'POST',
    credentials: 'include',
    body: formData,
  })

  if (!response.ok) {
    throw new Error(`Shelf photo upload failed (${response.status}).`)
  }

  return response.json() as Promise<ScanSessionResponse>
}

export async function getLatestScanSession(): Promise<ScanSessionResponse | null> {
  const response = await fetch('/api/family/current/scan-sessions/latest', {
    credentials: 'include',
  })

  if (response.status === 404) {
    return null
  }

  if (!response.ok) {
    throw new Error(`Latest scan session lookup failed (${response.status}).`)
  }

  return response.json() as Promise<ScanSessionResponse>
}

export async function getScanSession(scanSessionId: string): Promise<ScanSessionResponse> {
  const response = await fetch(`/api/family/current/scan-sessions/${scanSessionId}`, { credentials: 'include' })
  if (!response.ok) throw new Error(`Scan session lookup failed (${response.status}).`)
  return response.json() as Promise<ScanSessionResponse>
}

export async function searchBookMetadata(title: string): Promise<BookMetadataCandidateResponse[]> {
  const response = await fetch(`/api/book-metadata/search?title=${encodeURIComponent(title)}`, { credentials: 'include' })
  if (!response.ok) throw new Error(`Book metadata search failed (${response.status}).`)
  const payload = await response.json() as { candidates: BookMetadataCandidateResponse[] }
  return payload.candidates
}

export async function updateScanCandidate(
  scanSessionId: string,
  candidateId: string,
  input: { displayTitle: string; confidenceLabel: string; author?: string; recognitionRank?: number; recognitionEvidence?: string; metadataMatches?: BookMetadataCandidateResponse[] },
): Promise<ScanSessionResponse> {
  const response = await fetch(`/api/family/current/scan-sessions/${scanSessionId}/candidates/${candidateId}`, {
    method: 'PUT',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  if (!response.ok) throw new Error(`Scan candidate update failed (${response.status}).`)
  return response.json() as Promise<ScanSessionResponse>
}

export async function discardScanCandidate(scanSessionId: string, candidateId: string): Promise<void> {
  const response = await fetch(`/api/family/current/scan-sessions/${scanSessionId}/candidates/${candidateId}`, {
    method: 'DELETE',
    credentials: 'include',
  })
  if (!response.ok) throw new Error(`Scan candidate discard failed (${response.status}).`)
}

export async function updateBookEditionVersion(
  bookEditionId: string,
  input: { isbn?: string; format?: string; publicationYear?: number },
): Promise<BookEditionVersionResponse> {
  const response = await fetch(`/api/family/current/book-editions/${bookEditionId}/version`, {
    method: 'PUT',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  if (!response.ok) throw new Error(`Book edition version update failed (${response.status}).`)
  return response.json() as Promise<BookEditionVersionResponse>
}

export async function purchaseScanCandidate(
  scanSessionId: string,
  candidateId: string,
  input: ConfirmScanPurchaseRequest,
): Promise<ScanPurchaseResponse> {
  const response = await fetch(`/api/family/current/scan-sessions/${scanSessionId}/candidates/${candidateId}/purchase`, {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })

  if (response.ok) return response.json() as Promise<ScanPurchaseResponse>

  let duplicate: DuplicateConfirmationResponse | undefined
  let message = `Scan candidate purchase failed (${response.status}).`
  try {
    const payload = await response.json() as DuplicateConfirmationResponse & { detail?: string }
    if (response.status === 409 && Array.isArray(payload.matches)) duplicate = payload
    message = payload.detail ?? payload.message ?? message
  } catch {
    // Keep the status-based fallback.
  }
  throw new ScanPurchaseError(response.status, message, duplicate)
}
