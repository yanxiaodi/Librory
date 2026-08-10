import * as React from 'react'
import { AlertCircle, Camera, CheckCircle2, Loader2, ScanSearch } from 'lucide-react'
import { PageFrame } from '@/components/shell/PageFrame'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { BookRecognitionResults } from '@/components/scans/BookRecognitionResults'
import { useAuthSession } from '@/auth/AuthSessionContext'
import { listMembers, type FamilyMember } from '@/lib/familyApi'
import {
  createBookRecognitionJob,
  getBookRecognitionJob,
  isRecognitionJobComplete,
  type BookRecognitionJobResponse,
} from '@/lib/bookRecognitionApi'
import { createScanSession, type ScanSessionResponse } from '@/lib/scansApi'

type ScanState = 'idle' | 'compressing' | 'uploading' | 'polling' | 'ready' | 'error'
type PersistenceState = 'idle' | 'saving' | 'saved' | 'error'

const stateCopy: Record<ScanState, { title: string; description: string; tone: string }> = {
  idle: {
    title: 'Ready for a shelf photo',
    description: 'Tap scan, take a photo, and the app will start an async recognition job.',
    tone: 'text-[var(--text-secondary)]',
  },
  compressing: {
    title: 'Preparing photo',
    description: 'Optimizing the image for upload.',
    tone: 'text-[var(--text-secondary)]',
  },
  uploading: {
    title: 'Uploading photo',
    description: 'Sending the image to the server now.',
    tone: 'text-[var(--text-secondary)]',
  },
  polling: {
    title: 'Polling recognition job',
    description: 'The image is stored. The app is checking for recognized book titles.',
    tone: 'text-[var(--text-secondary)]',
  },
  ready: {
    title: 'Recognition complete',
    description: 'Review the recognized titles and their metadata matches.',
    tone: 'text-[var(--text-secondary)]',
  },
  error: {
    title: 'Recognition failed',
    description: 'Try the shelf photo again. The job did not complete successfully.',
    tone: 'text-[var(--text-secondary)]',
  },
}

function toDetectedLanguage(language: string | null) {
  if (language?.toLowerCase() === 'en') return 0
  if (language?.toLowerCase() === 'zh') return 1
  return undefined
}

function languageLabel(language: number | null) {
  if (language === 0) return 'English'
  if (language === 1) return 'Chinese'
  return 'Unknown language'
}

export async function compressImageToJpeg(file: File): Promise<File> {
  if (file.type === 'image/jpeg' && file.size <= 10 * 1024 * 1024) {
    return file
  }

  const imageUrl = URL.createObjectURL(file)

  try {
    const image = await new Promise<HTMLImageElement>((resolve, reject) => {
      const element = new Image()
      element.onload = () => resolve(element)
      element.onerror = () => reject(new Error('Failed to load image for compression.'))
      element.src = imageUrl
    })

    const maxDimension = 1600
    const scale = Math.min(1, maxDimension / Math.max(image.width, image.height))
    const width = Math.max(1, Math.round(image.width * scale))
    const height = Math.max(1, Math.round(image.height * scale))

    const canvas = document.createElement('canvas')
    canvas.width = width
    canvas.height = height

    const context = canvas.getContext('2d')
    if (!context) {
      throw new Error('Image compression is not supported in this browser.')
    }

    context.drawImage(image, 0, 0, width, height)

    const blob = await new Promise<Blob>((resolve, reject) => {
      canvas.toBlob(result => {
        if (!result) {
          reject(new Error('Failed to compress image.'))
          return
        }

        resolve(result)
      }, 'image/jpeg', 0.82)
    })

    const baseName = file.name.replace(/\.[^.]+$/, '') || 'photo'
    return new File([blob], `${baseName}.jpg`, { type: 'image/jpeg', lastModified: file.lastModified })
  } finally {
    URL.revokeObjectURL(imageUrl)
  }
}

export function ScansPage() {
  const { family, user } = useAuthSession()
  const inputRef = React.useRef<HTMLInputElement>(null)
  const [state, setState] = React.useState<ScanState>('idle')
  const [fileName, setFileName] = React.useState<string | null>(null)
  const [job, setJob] = React.useState<BookRecognitionJobResponse | null>(null)
  const [members, setMembers] = React.useState<FamilyMember[]>([])
  const [selectedMemberId, setSelectedMemberId] = React.useState(family?.memberId ?? '')
  const [memberError, setMemberError] = React.useState<string | null>(null)
  const [scanSession, setScanSession] = React.useState<ScanSessionResponse | null>(null)
  const [persistenceState, setPersistenceState] = React.useState<PersistenceState>('idle')
  const [persistenceError, setPersistenceError] = React.useState<string | null>(null)
  const [reviewedCandidates, setReviewedCandidates] = React.useState<BookRecognitionJobResponse['candidates']>([])
  const [uploadError, setUploadError] = React.useState<string | null>(null)
  const [debugMessage, setDebugMessage] = React.useState<string | null>(null)
  const [sessionMarker] = React.useState(() => `scan-${Date.now().toString(36)}`)
  const [reviewedCandidatesInitialized, setReviewedCandidatesInitialized] = React.useState(false)
  const pollTimerRef = React.useRef<number | null>(null)
  const activeJobIdRef = React.useRef<string | null>(null)
  const activeTargetMemberIdRef = React.useRef<string | undefined>(undefined)

  const currentMemberId = family?.memberId

  React.useEffect(() => {
    void listMembers()
      .then(result => {
        const eligible = result.filter(member =>
          member.memberId === currentMemberId || (member.isActive && member.canUseForFamilyRecommendations === true),
        )
        setMembers(eligible)
        setSelectedMemberId(previous => {
          if (eligible.some(member => member.memberId === previous)) return previous
          if (currentMemberId && eligible.some(member => member.memberId === currentMemberId)) return currentMemberId
          return eligible[0]?.memberId ?? currentMemberId ?? ''
        })
        setMemberError(null)
      })
      .catch(() => {
        setMemberError('Family members could not be loaded. Scanning will use the current member.')
        if (currentMemberId) setSelectedMemberId(currentMemberId)
      })
  }, [currentMemberId])

  const clearPollTimer = React.useCallback(() => {
    if (pollTimerRef.current !== null) {
      window.clearTimeout(pollTimerRef.current)
      pollTimerRef.current = null
    }
  }, [])

  const persistScanSession = React.useCallback(async (completedJob: BookRecognitionJobResponse) => {
    if (activeJobIdRef.current !== completedJob.jobId) return

    setPersistenceState('saving')
    setPersistenceError(null)

    try {
      const candidatesToPersist = reviewedCandidatesInitialized ? reviewedCandidates : completedJob.candidates
      const response = await createScanSession({
        shelfPhotoPath: completedJob.sourcePhotoPath,
        targetMemberId: activeTargetMemberIdRef.current,
        candidates: candidatesToPersist.map(candidate => ({
          displayTitle: candidate.displayTitle,
          confidenceLabel: candidate.evidenceText,
          author: candidate.metadataMatches[0]?.authors[0],
          recommendationScore: Math.min(Math.max(candidate.rank / 1000, 0), 1),
          detectedLanguage: toDetectedLanguage(candidate.metadataMatches[0]?.language ?? null),
        })),
      })
      if (activeJobIdRef.current !== completedJob.jobId) return
      setScanSession(response)
      setPersistenceState('saved')
    } catch {
      if (activeJobIdRef.current !== completedJob.jobId) return
      setPersistenceState('error')
      setPersistenceError('The scan results are ready, but the member context could not be saved.')
    }
  }, [reviewedCandidates, reviewedCandidatesInitialized])

  const schedulePoll = React.useCallback(async (jobId: string) => {
    try {
      const current = await getBookRecognitionJob(jobId)
      if (activeJobIdRef.current !== jobId) {
        return
      }

      setJob(current)

      if (isRecognitionJobComplete(current.status)) {
        setState(current.status === 3 ? 'error' : 'ready')
        setReviewedCandidates(current.candidates)
        setReviewedCandidatesInitialized(true)
        clearPollTimer()
        if (current.status === 2) void persistScanSession(current)
        return
      }

      setState('polling')
      clearPollTimer()
      pollTimerRef.current = window.setTimeout(() => {
        void schedulePoll(jobId)
      }, 1000)
    } catch (error) {
      if (activeJobIdRef.current !== jobId) {
        return
      }

      setUploadError(error instanceof Error ? error.message : 'Book recognition job lookup failed.')
      setState('error')
      clearPollTimer()
    }
  }, [clearPollTimer, persistScanSession])

  const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''

    if (!file) {
      return
    }

    setFileName(file.name)
    setJob(null)
    setReviewedCandidates([])
    setReviewedCandidatesInitialized(false)
    setScanSession(null)
    setPersistenceState('idle')
    setPersistenceError(null)
    setDebugMessage(null)
    activeJobIdRef.current = null
    activeTargetMemberIdRef.current = selectedMemberId || undefined
    setState('compressing')

    try {
      const uploadFile = await compressImageToJpeg(file)
      setState('uploading')
      const response = await createBookRecognitionJob(uploadFile)
      setJob(response)
      activeJobIdRef.current = response.jobId

      if (isRecognitionJobComplete(response.status)) {
        setState(response.status === 3 ? 'error' : 'ready')
        setReviewedCandidates(response.candidates)
        setReviewedCandidatesInitialized(true)
        if (response.status === 2) void persistScanSession(response)
        return
      }

      setState('polling')
      clearPollTimer()
      void schedulePoll(response.jobId)
    } catch (error) {
      setUploadError(error instanceof Error ? error.message : 'Book recognition upload failed.')
      setDebugMessage(error instanceof Error ? error.message : 'Book recognition upload failed.')
      setState('error')
    }
  }

  const retryPersistence = () => {
    if (job?.status === 2) void persistScanSession(job)
  }

  React.useEffect(() => {
    return () => {
      activeJobIdRef.current = null
      clearPollTimer()
    }
  }, [clearPollTimer])

  const status = stateCopy[state]

  return (
    <PageFrame
      eyebrow="Scans"
      title="Shelf recognition"
      description="Take a shelf photo from your phone, upload it, and let recognition continue asynchronously."
    >
      <div className="grid gap-4">
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2 text-[var(--accent)]">
              {state === 'uploading' ? <Loader2 className="h-5 w-5 animate-spin" strokeWidth={1.8} /> : <Camera className="h-5 w-5" strokeWidth={1.8} />}
              <CardTitle>{status.title}</CardTitle>
            </div>
            <CardDescription className={status.tone}>
              {status.description}
            </CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4">
            <div className="grid gap-2">
              <label className="text-sm font-semibold text-[var(--text-secondary)]" htmlFor="scan-target-member">
                Scan for member
              </label>
              <select
                id="scan-target-member"
                value={selectedMemberId}
                onChange={event => setSelectedMemberId(event.target.value)}
                disabled={state === 'compressing' || state === 'uploading' || state === 'polling' || persistenceState === 'saving' || persistenceState === 'error' || members.length === 0}
                className="h-12 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 text-[var(--text-primary)] outline-none focus:ring-2 focus:ring-[var(--accent-subtle)]"
              >
                {members.length === 0 && currentMemberId ? <option value={currentMemberId}>{user?.displayName ?? 'Current member'}</option> : null}
                {members.map(member => <option key={member.memberId} value={member.memberId}>{member.displayName}</option>)}
              </select>
              {memberError ? <p className="text-sm leading-6 text-[var(--text-secondary)]">{memberError}</p> : null}
            </div>

            <input
              id="shelf-photo-input"
              ref={inputRef}
              aria-label="Shelf photo"
              accept="image/*"
              capture="environment"
              className="sr-only"
              disabled={state === 'compressing' || state === 'uploading' || state === 'polling'}
              onChange={handleFileChange}
              type="file"
            />

            <Button
              type="button"
              size="lg"
              onClick={() => {
                if (state === 'compressing' || state === 'uploading' || state === 'polling') {
                  return
                }

                inputRef.current?.click()
              }}
              disabled={state === 'compressing' || state === 'uploading' || state === 'polling'}
            >
              <ScanSearch className="h-[18px] w-[18px]" strokeWidth={1.5} />
              {state === 'compressing' ? 'Preparing...' : state === 'uploading' ? 'Uploading...' : 'Scan a Shelf'}
            </Button>

            <div className="rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-sunken)] px-4 py-3">
              <div className="flex items-center gap-2 text-sm font-medium text-[var(--text-primary)]">
                {state === 'polling' || state === 'ready' ? (
                  <CheckCircle2 className="h-4 w-4 text-[var(--accent)]" strokeWidth={1.8} />
                ) : state === 'error' ? (
                  <AlertCircle className="h-4 w-4 text-[var(--accent)]" strokeWidth={1.8} />
                ) : (
                  <ScanSearch className="h-4 w-4 text-[var(--accent)]" strokeWidth={1.8} />
                )}
                <span>{state === 'idle' ? 'Use the camera on your phone for the fastest capture.' : 'Current recognition status'}</span>
              </div>
              <p className="mt-2 text-sm leading-6 text-[var(--text-secondary)]">
                {fileName ? `Selected file: ${fileName}` : 'Take a single shelf photo with the books facing the camera.'}
              </p>
              {job ? (
                <p className="mt-2 text-sm leading-6 text-[var(--text-secondary)]">
                  Recognition job: <span className="font-medium text-[var(--text-primary)]">{job.jobId}</span>
                </p>
              ) : null}
            </div>

            {state === 'error' ? (
              <p className="text-sm leading-6 text-[var(--text-secondary)]">
                The recognition job did not complete. Tap scan again to retry.
              </p>
            ) : null}
            {uploadError ? (
              <p className="text-sm leading-6 text-[var(--text-secondary)]">
                Error: {uploadError}
              </p>
            ) : null}
            {debugMessage ? (
              <p className="text-sm leading-6 text-[var(--text-secondary)]">
                Debug: {debugMessage}
              </p>
            ) : null}
            <p className="text-xs leading-5 text-[var(--text-tertiary)]">
              Session: {sessionMarker}
            </p>
          </CardContent>
        </Card>

        {persistenceState === 'saving' ? <Card><CardContent><p className="text-sm text-[var(--text-secondary)]">Saving scan context…</p></CardContent></Card> : null}
        {persistenceState === 'error' ? (
          <Card>
            <CardContent className="grid gap-3">
              <p className="text-sm leading-6 text-[var(--text-secondary)]">{persistenceError}</p>
              <Button type="button" variant="outline" onClick={retryPersistence}>Retry saving context</Button>
            </CardContent>
          </Card>
        ) : null}
        {scanSession ? (
          <Card>
            <CardHeader>
              <CardTitle>Recommendation context</CardTitle>
              <CardDescription>Scan prepared for {scanSession.targetMemberDisplayName || 'the current member'}.</CardDescription>
            </CardHeader>
            <CardContent className="grid gap-2 text-sm leading-6 text-[var(--text-secondary)]">
              <p>Profile: {scanSession.targetProfileUsed ? 'used' : scanSession.targetProfileAvailable ? 'available but not used' : 'not available'}</p>
              <p>Language context: {languageLabel(scanSession.inferredLanguage)}</p>
              {scanSession.hasMixedLanguages ? <p>This shelf contains mixed languages; each book will be considered independently.</p> : null}
            </CardContent>
          </Card>
        ) : null}

        {job && state !== 'compressing' && state !== 'uploading' ? <BookRecognitionResults job={job} candidates={reviewedCandidates} onCandidatesChange={setReviewedCandidates} /> : null}
        {job && state === 'polling' ? (
          <Card>
            <CardContent className="grid gap-2">
              <p className="text-sm font-medium text-[var(--text-primary)]">Recognition job is running</p>
              <p className="text-sm leading-6 text-[var(--text-secondary)]">
                Job {job.jobId} is queued or being processed. If it stays here for a long time, check the API logs for OCR or Azure OpenAI errors.
              </p>
            </CardContent>
          </Card>
        ) : null}
      </div>
    </PageFrame>
  )
}
