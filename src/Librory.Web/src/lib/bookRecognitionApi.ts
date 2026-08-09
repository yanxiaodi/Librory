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
  console.log('[Librory] createBookRecognitionJob called', {
    name: file.name,
    type: file.type,
    size: file.size,
  })

  const formData = new FormData()
  formData.append('photo', file)

  const response = await fetch('/api/book-recognition-jobs', {
    method: 'POST',
    credentials: 'include',
    body: formData,
  })

  console.log('[Librory] createBookRecognitionJob response', {
    ok: response.ok,
    status: response.status,
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response, `Book recognition job creation failed (${response.status}).`))
  }

  return response.json() as Promise<BookRecognitionJobResponse>
}

export async function getBookRecognitionJob(jobId: string): Promise<BookRecognitionJobResponse> {
  const response = await fetch(`/api/book-recognition-jobs/${jobId}`, {
    credentials: 'include',
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response, `Book recognition job lookup failed (${response.status}).`))
  }

  return response.json() as Promise<BookRecognitionJobResponse>
}

export function isRecognitionJobComplete(status: number) {
  return status === 2 || status === 3
}

async function readErrorMessage(response: Response, fallbackMessage: string) {
  const contentType = response.headers.get('Content-Type') ?? ''

  if (contentType.includes('application/json')) {
    try {
      const payload = await response.json() as { detail?: string; title?: string; message?: string }
      return payload.detail ?? payload.title ?? payload.message ?? fallbackMessage
    } catch {
      return fallbackMessage
    }
  }

  try {
    const text = await response.text()
    return text.trim() || fallbackMessage
  } catch {
    return fallbackMessage
  }
}
