import { useEffect, useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { listFamilies, selectFamily, type FamilySummary } from '@/lib/familyApi'

type FamilySectionProps = {
  currentFamilyId: string | undefined
  onFamilySelected: () => Promise<void>
}

export function FamilySection({ currentFamilyId, onFamilySelected }: FamilySectionProps) {
  const [families, setFamilies] = useState<FamilySummary[]>([])
  const [selectedId, setSelectedId] = useState(currentFamilyId ?? '')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    void listFamilies()
      .then(result => {
        if (!cancelled) {
          setFamilies(result)
          setSelectedId(currentFamilyId ?? result[0]?.familyId ?? '')
        }
      })
      .catch(() => {
        if (!cancelled) setError('Unable to load your families.')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [currentFamilyId])

  const handleSelect = async (familyId: string) => {
    setSelectedId(familyId)
    setSaving(true)
    setError(null)
    try {
      await selectFamily(familyId)
      await onFamilySelected()
    } catch {
      setError('Unable to switch families.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Family</CardTitle>
        <p className="text-sm leading-6 text-[var(--text-secondary)]">Choose which family library is active.</p>
      </CardHeader>
      <CardContent className="pt-0">
        {loading ? <p className="text-sm text-[var(--text-tertiary)]">Loading families…</p> : null}
        {!loading && families.length === 0 ? <p className="text-sm text-[var(--text-secondary)]">No active families found.</p> : null}
        {families.length > 0 ? (
          <label className="grid gap-2 text-sm font-semibold text-[var(--text-secondary)]" htmlFor="current-family">
            Current family
            <select
              id="current-family"
              value={selectedId}
              disabled={saving}
              onChange={event => void handleSelect(event.target.value)}
              className="h-12 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 font-normal text-[var(--text-primary)] outline-none focus:ring-2 focus:ring-[var(--accent-subtle)]"
            >
              {families.map(family => <option key={family.familyId} value={family.familyId}>{family.familyName}</option>)}
            </select>
          </label>
        ) : null}
        {saving ? <p className="mt-3 text-sm text-[var(--text-tertiary)]">Switching family…</p> : null}
        {error ? <p role="alert" className="mt-3 text-sm text-[var(--status-alert)]">{error}</p> : null}
      </CardContent>
    </Card>
  )
}
