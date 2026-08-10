import * as React from 'react'
import { createPortal } from 'react-dom'
import { AlertCircle, Check, Loader2, RotateCcw, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

export interface ShelfCameraCaptureProps {
  onCapture: (file: File) => void
  onCancel: () => void
}

type CaptureState = 'initializing' | 'error' | 'live' | 'review'

function describeCameraError(error: unknown): string {
  const name = error instanceof DOMException ? error.name : ''

  if (name === 'NotAllowedError') {
    return 'Camera permission was denied. Allow camera access and try again, or choose a photo from your library.'
  }
  if (name === 'NotFoundError') {
    return 'No camera was found on this device. Choose a photo from your library instead.'
  }
  if (name === 'NotReadableError') {
    return 'The camera is already in use by another app. Close it and try again.'
  }

  return 'Could not access the camera. Choose a photo from your library instead.'
}

// Captures the shelf photo from an in-page video preview instead of handing off to the
// native camera app. Handing off backgrounds the browser tab, and mobile OSes sometimes
// reload that backgrounded tab before the handoff returns, silently losing the upload.
export function ShelfCameraCapture({ onCapture, onCancel }: ShelfCameraCaptureProps) {
  const videoRef = React.useRef<HTMLVideoElement>(null)
  const canvasRef = React.useRef<HTMLCanvasElement>(null)
  const streamRef = React.useRef<MediaStream | null>(null)
  const reviewImageRef = React.useRef<{ file: File; url: string } | null>(null)

  const [state, setState] = React.useState<CaptureState>('initializing')
  const [errorMessage, setErrorMessage] = React.useState('')
  const [facingMode, setFacingMode] = React.useState<'user' | 'environment'>('environment')
  const [reviewImage, setReviewImage] = React.useState<{ file: File; url: string } | null>(null)

  const stopCamera = React.useCallback(() => {
    streamRef.current?.getTracks().forEach(track => track.stop())
    streamRef.current = null
  }, [])

  const startCamera = React.useCallback(async (mode: 'user' | 'environment') => {
    setState('initializing')
    setErrorMessage('')
    stopCamera()

    try {
      if (!navigator.mediaDevices?.getUserMedia) {
        throw new Error('Camera API not supported in this browser.')
      }

      let stream: MediaStream
      try {
        stream = await navigator.mediaDevices.getUserMedia({
          video: { facingMode: mode, width: { ideal: 1920 }, height: { ideal: 1080 } },
          audio: false,
        })
      } catch {
        // Some devices reject the facingMode/resolution hints; retry with plain video.
        stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: false })
      }

      streamRef.current = stream
      if (videoRef.current) {
        videoRef.current.srcObject = stream
      }
      setState('live')
    } catch (error) {
      setErrorMessage(describeCameraError(error))
      setState('error')
    }
  }, [stopCamera])

  React.useEffect(() => {
    const originalOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    void startCamera(facingMode)

    return () => {
      document.body.style.overflow = originalOverflow
      stopCamera()
      if (reviewImageRef.current) {
        URL.revokeObjectURL(reviewImageRef.current.url)
      }
    }
    // Mount-only: startCamera/stopCamera are stable refs to the current facingMode is
    // read explicitly by switchCamera below rather than re-running this effect.
  }, [])

  React.useEffect(() => {
    reviewImageRef.current = reviewImage
  }, [reviewImage])

  const switchCamera = () => {
    const nextMode = facingMode === 'environment' ? 'user' : 'environment'
    setFacingMode(nextMode)
    void startCamera(nextMode)
  }

  const capturePhoto = () => {
    const video = videoRef.current
    const canvas = canvasRef.current
    if (!video || !canvas || video.videoWidth === 0) return

    canvas.width = video.videoWidth
    canvas.height = video.videoHeight

    const context = canvas.getContext('2d')
    if (!context) return

    context.drawImage(video, 0, 0, canvas.width, canvas.height)

    canvas.toBlob(blob => {
      if (!blob) return

      const file = new File([blob], `shelf-${Date.now()}.jpg`, { type: 'image/jpeg' })
      setReviewImage({ file, url: URL.createObjectURL(blob) })
      setState('review')
    }, 'image/jpeg', 0.9)
  }

  const retake = () => {
    if (reviewImage) URL.revokeObjectURL(reviewImage.url)
    setReviewImage(null)
    setState('live')
  }

  const confirmPhoto = () => {
    if (!reviewImage) return
    const { file, url } = reviewImage
    URL.revokeObjectURL(url)
    setReviewImage(null)
    stopCamera()
    onCapture(file)
  }

  const handleCancel = () => {
    if (reviewImage) URL.revokeObjectURL(reviewImage.url)
    stopCamera()
    onCancel()
  }

  // Render outside the page's DOM tree (which sits inside a scrollable `main`):
  // iOS Safari can mis-hit-test fixed-position descendants of a scrolling
  // ancestor, letting a tap fall through to whatever is underneath once this
  // overlay closes.
  return createPortal(
    <div className="fixed inset-0 z-50 bg-black">
      <video
        ref={videoRef}
        autoPlay
        playsInline
        muted
        className={cn('absolute inset-0 h-full w-full object-cover', state === 'live' ? '' : 'invisible')}
      />
      <canvas ref={canvasRef} className="hidden" />

      {state === 'review' && reviewImage ? (
        <img src={reviewImage.url} alt="Captured shelf preview" className="absolute inset-0 h-full w-full object-cover" />
      ) : null}

      {state === 'initializing' ? (
        <div className="absolute inset-0 flex flex-col items-center justify-center gap-4 px-6 text-center text-white">
          <Loader2 className="h-8 w-8 animate-spin" strokeWidth={1.8} />
          <p className="text-sm">Starting camera…</p>
          <Button type="button" variant="outline" onClick={handleCancel}>Cancel</Button>
        </div>
      ) : null}

      {state === 'error' ? (
        <div className="absolute inset-0 flex flex-col items-center justify-center gap-4 px-6 text-center text-white">
          <AlertCircle className="h-8 w-8" strokeWidth={1.8} />
          <p className="text-sm leading-6">{errorMessage}</p>
          <div className="flex gap-3">
            <Button type="button" variant="outline" onClick={() => void startCamera(facingMode)}>Retry</Button>
            <Button type="button" onClick={handleCancel}>Choose from library</Button>
          </div>
        </div>
      ) : null}

      {state === 'live' ? (
        <div className="absolute inset-x-0 bottom-0 flex items-center justify-between px-8 pb-[calc(env(safe-area-inset-bottom)+1.5rem)] pt-4">
          <button
            type="button"
            aria-label="Cancel"
            onClick={handleCancel}
            className="flex h-12 w-12 items-center justify-center rounded-full bg-white/15 text-white"
          >
            <X className="h-6 w-6" strokeWidth={1.8} />
          </button>
          <button
            type="button"
            aria-label="Capture photo"
            onClick={capturePhoto}
            className="h-16 w-16 rounded-full border-4 border-white bg-white/25"
          />
          <button
            type="button"
            aria-label="Switch camera"
            onClick={switchCamera}
            className="flex h-12 w-12 items-center justify-center rounded-full bg-white/15 text-white"
          >
            <RotateCcw className="h-6 w-6" strokeWidth={1.8} />
          </button>
        </div>
      ) : null}

      {state === 'review' ? (
        <div className="absolute inset-x-0 bottom-0 flex items-center justify-center gap-4 px-8 pb-[calc(env(safe-area-inset-bottom)+1.5rem)] pt-4">
          <Button type="button" variant="outline" onClick={retake}>
            <RotateCcw className="h-[18px] w-[18px]" strokeWidth={1.5} />
            Retake
          </Button>
          <Button type="button" onClick={confirmPhoto}>
            <Check className="h-[18px] w-[18px]" strokeWidth={1.5} />
            Use Photo
          </Button>
        </div>
      ) : null}
    </div>,
    document.body,
  )
}
