import { FormEvent, useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { createInvitation, listInvitations, resendInvitation, revokeInvitation, type FamilyInvitation } from '@/lib/familyApi'

type InvitationsSectionProps = { isAdmin: boolean }

export function InvitationsSection({ isAdmin }: InvitationsSectionProps) {
  const [invitations, setInvitations] = useState<FamilyInvitation[]>([])
  const [email, setEmail] = useState('')
  const [oneTimeUrl, setOneTimeUrl] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [copied, setCopied] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const refresh = async (clearOneTimeUrl = true) => {
    setLoading(true)
    try {
      setInvitations(await listInvitations())
      if (clearOneTimeUrl) {
        setOneTimeUrl(null)
        setCopied(false)
      }
      setError(null)
    } catch {
      setError('Unable to load invitations.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { void refresh() }, [])

  const handleCreate = async (event: FormEvent) => {
    event.preventDefault()
    if (!email.trim()) return
    setSaving(true)
    try {
      const invitation = await createInvitation({ email: email.trim() })
      setEmail('')
      setOneTimeUrl(invitation.invitationUrl ?? null)
      setCopied(false)
      await refresh(false)
    } catch {
      setError('Unable to send this invitation.')
    } finally {
      setSaving(false)
    }
  }

  const handleResend = async (invitationId: string) => {
    setSaving(true)
    try {
      const invitation = await resendInvitation(invitationId)
      setOneTimeUrl(invitation.invitationUrl ?? null)
      setCopied(false)
      await refresh(false)
    } catch {
      setError('Unable to resend this invitation.')
    } finally {
      setSaving(false)
    }
  }

  const handleRevoke = async (invitationId: string) => {
    setSaving(true)
    try {
      await revokeInvitation(invitationId)
      await refresh()
    } catch {
      setError('Unable to revoke this invitation.')
    } finally {
      setSaving(false)
    }
  }

  const copyUrl = async () => {
    if (!oneTimeUrl) return
    await navigator.clipboard.writeText(oneTimeUrl)
    setCopied(true)
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Invitations</CardTitle>
        <p className="text-sm leading-6 text-[var(--text-secondary)]">Invite someone to join this family library.</p>
      </CardHeader>
      <CardContent className="grid gap-4 pt-0">
        {loading ? <p className="text-sm text-[var(--text-tertiary)]">Loading invitations…</p> : null}
        <div className="grid gap-2">
          {invitations.map(invitation => (
            <div key={invitation.invitationId} className="flex items-center justify-between gap-3 rounded-[var(--radius-md)] border border-[var(--border-subtle)] px-3 py-3">
              <div className="min-w-0">
                <p className="truncate text-sm font-semibold text-[var(--text-primary)]">{invitation.email}</p>
                <p className="text-xs text-[var(--text-secondary)]">{invitation.status} · Expires {new Date(invitation.expiresAt).toLocaleDateString()}</p>
              </div>
              {isAdmin && invitation.status === 'Pending' ? (
                <div className="flex shrink-0 gap-2">
                  <Button type="button" variant="outline" disabled={saving} onClick={() => void handleResend(invitation.invitationId)}>Resend</Button>
                  <Button type="button" variant="outline" disabled={saving} onClick={() => void handleRevoke(invitation.invitationId)}>Revoke</Button>
                </div>
              ) : null}
            </div>
          ))}
        </div>
        {isAdmin ? (
          <form className="grid gap-2 border-t border-[var(--border-subtle)] pt-4" onSubmit={event => void handleCreate(event)}>
            <label className="grid gap-2 text-sm font-semibold text-[var(--text-secondary)]" htmlFor="invitee-email">
              Invitee email
              <input id="invitee-email" type="email" value={email} onChange={event => setEmail(event.target.value)} placeholder="name@example.com" className="h-12 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 font-normal text-[var(--text-primary)] outline-none focus:ring-2 focus:ring-[var(--accent-subtle)]" />
            </label>
            <Button type="submit" disabled={saving || !email.trim()}>Send invitation</Button>
          </form>
        ) : null}
        {oneTimeUrl ? (
          <div className="grid gap-2 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--accent-muted)] p-3">
            <p className="text-xs font-semibold uppercase tracking-[0.14em] text-[var(--text-tertiary)]">One-time invitation link</p>
            <p className="break-all text-sm text-[var(--text-secondary)]">{oneTimeUrl}</p>
            <Button type="button" variant="outline" onClick={() => void copyUrl()}>{copied ? 'Copied' : 'Copy invitation link'}</Button>
          </div>
        ) : null}
        {error ? <p role="alert" className="text-sm text-[var(--status-alert)]">{error}</p> : null}
      </CardContent>
    </Card>
  )
}
