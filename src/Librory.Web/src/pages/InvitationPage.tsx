import { useEffect, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { BookOpen, Landmark } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader } from '@/components/ui/card'
import { useAuthSession, useAuthSessionActions } from '@/auth/AuthSessionContext'
import { acceptInvitation, getInvitationPreview, selectFamily, type InvitationPreview } from '@/lib/familyApi'
import { authEndpoints } from '@/auth/authEndpoints'

export default function InvitationPage() {
  const { token } = useParams<{ token: string }>()
  const location = useLocation()
  const navigate = useNavigate()
  const session = useAuthSession()
  const { refreshSession } = useAuthSessionActions()
  const [preview, setPreview] = useState<InvitationPreview | null>(null)
  const [loading, setLoading] = useState(true)
  const [accepting, setAccepting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!token) {
      setError('This invitation link is incomplete.')
      setLoading(false)
      return
    }
    let cancelled = false
    void getInvitationPreview(token)
      .then(result => { if (!cancelled) setPreview(result) })
      .catch(() => { if (!cancelled) setError('This invitation is invalid, expired, or no longer available.') })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [token])

  const returnUrl = `${location.pathname}${location.search}`
  const providerUrl = (provider: string) => `${provider}?returnUrl=${encodeURIComponent(returnUrl)}`

  const handleAccept = async () => {
    if (!token) return
    setAccepting(true)
    setError(null)
    try {
      const family = await acceptInvitation(token)
      await selectFamily(family.familyId)
      await refreshSession()
      navigate('/app/home', { replace: true })
    } catch {
      setError('This invitation could not be accepted. Check that the invitation email matches your account.')
    } finally {
      setAccepting(false)
    }
  }

  return (
    <main className="min-h-screen bg-[var(--page-bg)] px-5 py-6 text-[var(--text-primary)]">
      <div className="mx-auto flex min-h-[calc(100vh-3rem)] max-w-md items-center">
        <Card className="w-full shadow-[0_12px_32px_rgba(58,48,42,0.05)]">
          <CardHeader>
            <p className="text-[11px] font-semibold uppercase tracking-[0.24em] text-[var(--text-tertiary)]">Librory</p>
            <h1 className="mt-2 font-[family-name:var(--font-display)] text-[1.8rem] font-normal italic text-[var(--text-primary)]">
              {preview ? `Join ${preview.familyName}` : 'Family invitation'}
            </h1>
            <CardDescription className="text-[var(--text-secondary)]">
              {loading ? 'Checking your invitation…' : preview ? `This invitation is for ${preview.email}. It expires ${new Date(preview.expiresAt).toLocaleDateString()}.` : error}
            </CardDescription>
          </CardHeader>
          <CardContent className="grid gap-3">
            {session.status === 'anonymous' && preview ? <>
              <Button asChild variant="outline" size="lg" className="justify-start px-4 text-[var(--text-primary)]"><a href={providerUrl(authEndpoints.googleStart)}><BookOpen className="h-4 w-4" />Continue with Google</a></Button>
              <Button asChild variant="outline" size="lg" className="justify-start px-4 text-[var(--text-primary)]"><a href={providerUrl(authEndpoints.microsoftStart)}><Landmark className="h-4 w-4" />Continue with Microsoft</a></Button>
            </> : null}
            {session.status === 'authenticated' && preview ? <Button type="button" size="lg" disabled={accepting} onClick={() => void handleAccept()}>{accepting ? 'Accepting invitation…' : 'Accept invitation'}</Button> : null}
            {error && preview ? <p role="alert" className="text-sm text-[var(--status-alert)]">{error}</p> : null}
          </CardContent>
        </Card>
      </div>
    </main>
  )
}
