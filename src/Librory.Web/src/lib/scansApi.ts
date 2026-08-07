export interface ScanSessionResponse {
  scanSessionId: string
  familyId: string
  shelfPhotoPath: string
  candidates: unknown[]
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
  recommendationScore?: number
  isAlreadyOwned?: boolean
  duplicateMessage?: string
  detectedLanguage?: number
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
