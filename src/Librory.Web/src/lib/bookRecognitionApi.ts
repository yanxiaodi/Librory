export interface BookMetadataCandidateResponse {
  source: string
  sourceId: string
  title: string
  subtitle: string | null
  authors: string[]
  publisher: string | null
  publishedDate: string | null
  language: string | null
  description: string | null
  isbn10: string | null
  isbn13: string | null
  thumbnailUrl: string | null
  infoUrl: string | null
}

export interface BookRecognitionCandidateResponse {
  candidateId: string
  displayTitle: string
  evidenceText: string
  rank: number
  metadataMatches: BookMetadataCandidateResponse[]
}

export interface BookRecognitionJobResponse {
  jobId: string
  familyId: string
  status: number
  sourcePhotoPath: string
  candidates: BookRecognitionCandidateResponse[]
  warnings: string[]
  failureMessage: string | null
  createdAt: string
  updatedAt: string
}

export async function createBookRecognitionJob(file: File): Promise<BookRecognitionJobResponse> {
  const formData = new FormData()
  formData.append('photo', file)

  const response = await fetch('/api/book-recognition-jobs', {
    method: 'POST',
    credentials: 'include',
    body: formData,
  })

  if (!response.ok) {
    throw new Error(`Book recognition job creation failed (${response.status}).`)
  }

  return response.json() as Promise<BookRecognitionJobResponse>
}

export async function getBookRecognitionJob(jobId: string): Promise<BookRecognitionJobResponse> {
  const response = await fetch(`/api/book-recognition-jobs/${jobId}`, {
    credentials: 'include',
  })

  if (!response.ok) {
    throw new Error(`Book recognition job lookup failed (${response.status}).`)
  }

  return response.json() as Promise<BookRecognitionJobResponse>
}

export function isRecognitionJobComplete(status: number) {
  return status === 2 || status === 3
}
