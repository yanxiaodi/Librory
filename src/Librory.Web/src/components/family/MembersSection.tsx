import { FormEvent, useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { createMember, listMembers, setMemberActive, type FamilyMember } from '@/lib/familyApi'

type MembersSectionProps = { isAdmin: boolean; refreshKey?: number }

export function MembersSection({ isAdmin, refreshKey = 0 }: MembersSectionProps) {
  const [members, setMembers] = useState<FamilyMember[]>([])
  const [name, setName] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const refresh = async () => {
    setLoading(true)
    try {
      setMembers(await listMembers())
      setError(null)
    } catch {
      setError('Unable to load family members.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { void refresh() }, [refreshKey])

  const handleCreate = async (event: FormEvent) => {
    event.preventDefault()
    if (!name.trim()) return
    setSaving(true)
    try {
      await createMember({ displayName: name.trim() })
      setName('')
      await refresh()
    } catch {
      setError('Unable to add this member.')
    } finally {
      setSaving(false)
    }
  }

  const handleActive = async (member: FamilyMember) => {
    setSaving(true)
    try {
      await setMemberActive(member.memberId, !member.isActive)
      await refresh()
    } catch {
      setError('Unable to update this member.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Members</CardTitle>
        <p className="text-sm leading-6 text-[var(--text-secondary)]">Manage the people who share this family library.</p>
      </CardHeader>
      <CardContent className="grid gap-4 pt-0">
        {loading ? <p className="text-sm text-[var(--text-tertiary)]">Loading members…</p> : null}
        <div className="grid gap-2">
          {members.map(member => (
            <div key={member.memberId} className="flex items-center justify-between gap-3 rounded-[var(--radius-md)] border border-[var(--border-subtle)] px-3 py-3">
              <div className="min-w-0">
                <p className="truncate text-sm font-semibold text-[var(--text-primary)]">{member.displayName}</p>
                <p className="text-xs text-[var(--text-secondary)]">{member.role} · {member.hasAccount ? 'Account linked' : 'Placeholder'} · {member.isActive ? 'Active' : 'Deactivated'}</p>
              </div>
              {isAdmin ? <Button type="button" variant="outline" disabled={saving} onClick={() => void handleActive(member)}>{member.isActive ? 'Deactivate' : 'Reactivate'}</Button> : null}
            </div>
          ))}
        </div>
        {isAdmin ? (
          <form className="grid gap-2 border-t border-[var(--border-subtle)] pt-4" onSubmit={event => void handleCreate(event)}>
            <label className="grid gap-2 text-sm font-semibold text-[var(--text-secondary)]" htmlFor="member-name">
              Add a placeholder member
              <input id="member-name" value={name} onChange={event => setName(event.target.value)} placeholder="Member name" className="h-12 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 font-normal text-[var(--text-primary)] outline-none focus:ring-2 focus:ring-[var(--accent-subtle)]" />
            </label>
            <Button type="submit" disabled={saving || !name.trim()}>Add member</Button>
          </form>
        ) : null}
        {error ? <p role="alert" className="text-sm text-[var(--status-alert)]">{error}</p> : null}
      </CardContent>
    </Card>
  )
}
