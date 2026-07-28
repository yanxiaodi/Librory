export interface ScanSessionResponse {
  scanSessionId: string
  familyId: string
  shelfPhotoPath: string
  candidates: unknown[]
  expiresAt: string
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
