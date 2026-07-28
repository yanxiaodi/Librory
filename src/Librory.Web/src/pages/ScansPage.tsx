import * as React from 'react'
import { AlertCircle, Camera, CheckCircle2, Loader2, ScanSearch } from 'lucide-react'
import { PageFrame } from '@/components/shell/PageFrame'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { uploadShelfPhoto } from '@/lib/scansApi'

type ScanState = 'idle' | 'uploading' | 'scanning' | 'error'

const stateCopy: Record<ScanState, { title: string; description: string; tone: string }> = {
  idle: {
    title: 'Ready for a shelf photo',
    description: 'Tap scan, take a photo, and the app will start a temporary scan session.',
    tone: 'text-[var(--text-secondary)]',
  },
  uploading: {
    title: 'Uploading photo',
    description: 'Sending the image to the server now.',
    tone: 'text-[var(--text-secondary)]',
  },
  scanning: {
    title: 'Scanning in progress',
    description: 'The image is stored. Recognition will continue in the background.',
    tone: 'text-[var(--text-secondary)]',
  },
  error: {
    title: 'Upload failed',
    description: 'Try the shelf photo again. The image never made it into the scan flow.',
    tone: 'text-[var(--text-secondary)]',
  },
}

export function ScansPage() {
  const inputRef = React.useRef<HTMLInputElement>(null)
  const [state, setState] = React.useState<ScanState>('idle')
  const [fileName, setFileName] = React.useState<string | null>(null)
  const [sessionId, setSessionId] = React.useState<string | null>(null)

  const openPicker = () => {
    inputRef.current?.click()
  }

  const handlePickerKeyDown = (event: React.KeyboardEvent<HTMLLabelElement>) => {
    if (state === 'uploading') {
      return
    }

    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault()
      openPicker()
    }
  }

  const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''

    if (!file) {
      return
    }

    setFileName(file.name)
    setSessionId(null)
    setState('uploading')

    try {
      const response = await uploadShelfPhoto(file)
      setSessionId(response.scanSessionId)
      setState('scanning')
    } catch {
      setState('error')
    }
  }

  const status = stateCopy[state]

  return (
    <PageFrame
      eyebrow="Scans"
      title="Shelf scans"
      description="Take a shelf photo from your phone, upload it, and let the scan pipeline start immediately."
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
            <input
              id="shelf-photo-input"
              ref={inputRef}
              aria-label="Shelf photo"
              accept="image/*"
              capture="environment"
              className="sr-only"
              disabled={state === 'uploading'}
              onChange={handleFileChange}
              type="file"
            />

            <Button asChild size="lg">
              <label
                htmlFor="shelf-photo-input"
                role="button"
                tabIndex={0}
                onKeyDown={handlePickerKeyDown}
                aria-disabled={state === 'uploading'}
                className={state === 'uploading' ? 'pointer-events-none opacity-50' : 'cursor-pointer'}
              >
                <ScanSearch className="h-[18px] w-[18px]" strokeWidth={1.5} />
                {state === 'uploading' ? 'Uploading...' : 'Scan a Shelf'}
              </label>
            </Button>

            <div className="rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-sunken)] px-4 py-3">
              <div className="flex items-center gap-2 text-sm font-medium text-[var(--text-primary)]">
                {state === 'scanning' ? (
                  <CheckCircle2 className="h-4 w-4 text-[var(--accent)]" strokeWidth={1.8} />
                ) : state === 'error' ? (
                  <AlertCircle className="h-4 w-4 text-[var(--accent)]" strokeWidth={1.8} />
                ) : (
                  <ScanSearch className="h-4 w-4 text-[var(--accent)]" strokeWidth={1.8} />
                )}
                <span>{state === 'idle' ? 'Use the camera on your phone for the fastest capture.' : 'Current upload status'}</span>
              </div>
              <p className="mt-2 text-sm leading-6 text-[var(--text-secondary)]">
                {fileName ? `Selected file: ${fileName}` : 'Take a single shelf photo with the books facing the camera.'}
              </p>
              {sessionId ? (
                <p className="mt-2 text-sm leading-6 text-[var(--text-secondary)]">
                  Scan session: <span className="font-medium text-[var(--text-primary)]">{sessionId}</span>
                </p>
              ) : null}
            </div>

            {state === 'error' ? (
              <p className="text-sm leading-6 text-[var(--text-secondary)]">
                The upload failed before the scan session could start. Tap scan again to retry.
              </p>
            ) : null}
          </CardContent>
        </Card>
      </div>
    </PageFrame>
  )
}
