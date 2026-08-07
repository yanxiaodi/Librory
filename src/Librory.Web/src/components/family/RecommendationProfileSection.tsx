import { FormEvent, useEffect, useMemo, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  FamilyApiError,
  getMemberRecommendationProfile,
  listMembers,
  updateMemberRecommendationProfile,
  type FamilyMember,
  type RecommendationProfile,
  type RecommendationProfileUpdate,
} from '@/lib/familyApi'

type RecommendationProfileSectionProps = {
  isAdmin: boolean
  currentMemberId?: string
  refreshKey?: number
}

type ProfileFormState = {
  minimumAge: string
  maximumAge: string
  favoriteAuthors: string
  excludedAuthors: string
  favoriteGenres: string
  excludedGenres: string
  favoriteStyles: string
  excludedStyles: string
  preferredBookLanguages: Array<'English' | 'Chinese'>
  preferenceNotes: string
  profileVisibility: 'Family' | 'Private'
  useInFamilyRecommendations: boolean
}

const emptyForm: ProfileFormState = {
  minimumAge: '',
  maximumAge: '',
  favoriteAuthors: '',
  excludedAuthors: '',
  favoriteGenres: '',
  excludedGenres: '',
  favoriteStyles: '',
  excludedStyles: '',
  preferredBookLanguages: [],
  preferenceNotes: '',
  profileVisibility: 'Family',
  useInFamilyRecommendations: true,
}

const inputClassName = 'h-12 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 font-normal text-[var(--text-primary)] outline-none focus:ring-2 focus:ring-[var(--accent-subtle)] disabled:cursor-not-allowed disabled:opacity-60'
const labelClassName = 'grid gap-2 text-sm font-semibold text-[var(--text-secondary)]'

function splitCommaList(value: string): string[] {
  return [...new Set(value.split(',').map(item => item.trim()).filter(Boolean))]
}

function joinList(value: string[]): string {
  return value.join(', ')
}

function isChineseLanguage(value: number | string): boolean {
  return value === 1 || value === 'Chinese'
}

function toForm(profile: RecommendationProfile | null): ProfileFormState {
  if (!profile) return emptyForm

  return {
    minimumAge: profile.minimumAge?.toString() ?? '',
    maximumAge: profile.maximumAge?.toString() ?? '',
    favoriteAuthors: joinList(profile.favoriteAuthors),
    excludedAuthors: joinList(profile.excludedAuthors),
    favoriteGenres: joinList(profile.favoriteGenres),
    excludedGenres: joinList(profile.excludedGenres),
    favoriteStyles: joinList(profile.favoriteStyles),
    excludedStyles: joinList(profile.excludedStyles),
    preferredBookLanguages: profile.preferredBookLanguages.filter(isChineseLanguage).length > 0
      ? ['Chinese', ...(profile.preferredBookLanguages.some(value => !isChineseLanguage(value)) ? ['English' as const] : [])]
      : profile.preferredBookLanguages.length > 0 ? ['English'] : [],
    preferenceNotes: profile.preferenceNotes ?? '',
    profileVisibility: profile.profileVisibility === 1 || profile.profileVisibility === 'Private' ? 'Private' : 'Family',
    useInFamilyRecommendations: profile.useInFamilyRecommendations,
  }
}

function toPayload(form: ProfileFormState): RecommendationProfileUpdate {
  const numberOrNull = (value: string) => value.trim() ? Number(value) : null

  return {
    minimumAge: numberOrNull(form.minimumAge),
    maximumAge: numberOrNull(form.maximumAge),
    favoriteAuthors: splitCommaList(form.favoriteAuthors),
    excludedAuthors: splitCommaList(form.excludedAuthors),
    favoriteGenres: splitCommaList(form.favoriteGenres),
    excludedGenres: splitCommaList(form.excludedGenres),
    favoriteStyles: splitCommaList(form.favoriteStyles),
    excludedStyles: splitCommaList(form.excludedStyles),
    preferredBookLanguages: form.preferredBookLanguages.map(language => language === 'Chinese' ? 1 : 0),
    preferenceNotes: form.preferenceNotes.trim() || null,
    profileVisibility: form.profileVisibility === 'Private' ? 1 : 0,
    useInFamilyRecommendations: form.useInFamilyRecommendations,
  }
}

function Field({ label, id, value, onChange, disabled, multiline = false }: {
  label: string
  id: string
  value: string
  onChange: (value: string) => void
  disabled: boolean
  multiline?: boolean
}) {
  return (
    <label className={labelClassName} htmlFor={id}>
      {label}
      {multiline ? (
        <textarea id={id} value={value} disabled={disabled} onChange={event => onChange(event.target.value)} rows={3} className={`${inputClassName} h-auto py-3`} />
      ) : (
        <input id={id} value={value} disabled={disabled} onChange={event => onChange(event.target.value)} className={inputClassName} />
      )}
    </label>
  )
}

export function RecommendationProfileSection({ isAdmin, currentMemberId, refreshKey = 0 }: RecommendationProfileSectionProps) {
  const [members, setMembers] = useState<FamilyMember[]>([])
  const [selectedMemberId, setSelectedMemberId] = useState(currentMemberId ?? '')
  const [form, setForm] = useState<ProfileFormState>(emptyForm)
  const [loadingMembers, setLoadingMembers] = useState(true)
  const [loadingProfile, setLoadingProfile] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [readOnly, setReadOnly] = useState(false)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    let cancelled = false
    setLoadingMembers(true)
    void listMembers()
      .then(result => {
        if (cancelled) return
        const activeMembers = result.filter(member => member.isActive)
        setMembers(activeMembers)
        setSelectedMemberId(previous => {
          if (activeMembers.some(member => member.memberId === previous)) return previous
          if (currentMemberId && activeMembers.some(member => member.memberId === currentMemberId)) return currentMemberId
          return activeMembers[0]?.memberId ?? ''
        })
        setError(null)
      })
      .catch(() => {
        if (!cancelled) setError('Unable to load recommendation members.')
      })
      .finally(() => {
        if (!cancelled) setLoadingMembers(false)
      })
    return () => { cancelled = true }
  }, [currentMemberId, refreshKey])

  useEffect(() => {
    if (!selectedMemberId) return
    let cancelled = false
    setLoadingProfile(true)
    setForm(emptyForm)
    setError(null)
    setSaved(false)
    setReadOnly(!isAdmin && selectedMemberId !== currentMemberId)
    void getMemberRecommendationProfile(selectedMemberId)
      .then(profile => {
        if (cancelled) return
        setForm(toForm(profile))
        setError(null)
      })
      .catch((caught: unknown) => {
        if (cancelled) return
        if (caught instanceof FamilyApiError && caught.status === 404) {
          setForm(emptyForm)
          setError(null)
          return
        }
        if (caught instanceof FamilyApiError && caught.status === 403) {
          setReadOnly(true)
          setForm(emptyForm)
          setError('This profile is not available to you.')
          return
        }
        setError('Unable to load this recommendation profile.')
      })
      .finally(() => {
        if (!cancelled) setLoadingProfile(false)
      })
    return () => { cancelled = true }
  }, [currentMemberId, isAdmin, refreshKey, selectedMemberId])

  const selectedMember = useMemo(
    () => members.find(member => member.memberId === selectedMemberId),
    [members, selectedMemberId],
  )
  const canEdit = !readOnly && (isAdmin || selectedMemberId === currentMemberId)
  const updateField = <K extends keyof ProfileFormState>(key: K, value: ProfileFormState[K]) => {
    setForm(previous => ({ ...previous, [key]: value }))
    setSaved(false)
  }

  const toggleLanguage = (language: 'English' | 'Chinese') => {
    const next = form.preferredBookLanguages.includes(language)
      ? form.preferredBookLanguages.filter(value => value !== language)
      : [...form.preferredBookLanguages, language]
    updateField('preferredBookLanguages', next)
  }

  const handleSave = async (event: FormEvent) => {
    event.preventDefault()
    if (!selectedMemberId || !canEdit) return
    setSaving(true)
    setSaved(false)
    setError(null)
    try {
      const profile = await updateMemberRecommendationProfile(selectedMemberId, toPayload(form))
      setForm(toForm(profile))
      setSaved(true)
    } catch {
      setError('Unable to save this recommendation profile.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Reading preferences</CardTitle>
        <p className="text-sm leading-6 text-[var(--text-secondary)]">Tune recommendations for a family member without changing the family library.</p>
      </CardHeader>
      <CardContent className="grid gap-4 pt-0">
        {loadingMembers || loadingProfile ? <p className="text-sm text-[var(--text-tertiary)]">Loading preferences…</p> : null}
        {!loadingMembers && members.length === 0 ? <p className="text-sm text-[var(--text-secondary)]">No active family members found.</p> : null}
        {members.length > 0 ? (
          <form className="grid gap-4" onSubmit={event => void handleSave(event)}>
            {members.length > 1 ? (
              <label className={labelClassName} htmlFor="recommendation-member">
                Member
                <select id="recommendation-member" value={selectedMemberId} disabled={saving} onChange={event => setSelectedMemberId(event.target.value)} className={inputClassName}>
                  {members.map(member => <option key={member.memberId} value={member.memberId}>{member.displayName}</option>)}
                </select>
              </label>
            ) : <p className="text-sm font-semibold text-[var(--text-primary)]">{selectedMember?.displayName}</p>}

            {error === 'This profile is not available to you.' ? <p role="alert" className="text-sm text-[var(--status-alert)]">{error}</p> : null}
            {error !== 'This profile is not available to you.' ? (
              <>
                <div className="grid grid-cols-2 gap-3">
                  <Field label="Minimum reading age" id="minimum-reading-age" value={form.minimumAge} disabled={!canEdit || loadingProfile} onChange={value => updateField('minimumAge', value)} />
                  <Field label="Maximum reading age" id="maximum-reading-age" value={form.maximumAge} disabled={!canEdit || loadingProfile} onChange={value => updateField('maximumAge', value)} />
                </div>
                <div className="grid gap-3">
                  <Field label="Favorite authors" id="favorite-authors" value={form.favoriteAuthors} disabled={!canEdit || loadingProfile} onChange={value => updateField('favoriteAuthors', value)} />
                  <Field label="Excluded authors" id="excluded-authors" value={form.excludedAuthors} disabled={!canEdit || loadingProfile} onChange={value => updateField('excludedAuthors', value)} />
                  <Field label="Favorite genres" id="favorite-genres" value={form.favoriteGenres} disabled={!canEdit || loadingProfile} onChange={value => updateField('favoriteGenres', value)} />
                  <Field label="Excluded genres" id="excluded-genres" value={form.excludedGenres} disabled={!canEdit || loadingProfile} onChange={value => updateField('excludedGenres', value)} />
                  <Field label="Favorite styles" id="favorite-styles" value={form.favoriteStyles} disabled={!canEdit || loadingProfile} onChange={value => updateField('favoriteStyles', value)} />
                  <Field label="Excluded styles" id="excluded-styles" value={form.excludedStyles} disabled={!canEdit || loadingProfile} onChange={value => updateField('excludedStyles', value)} />
                </div>
                <fieldset className="grid gap-2">
                  <legend className={labelClassName}>Preferred book languages</legend>
                  <label className="flex items-center gap-2 text-sm text-[var(--text-primary)]"><input type="checkbox" checked={form.preferredBookLanguages.includes('English')} disabled={!canEdit || loadingProfile} onChange={() => toggleLanguage('English')} /> English</label>
                  <label className="flex items-center gap-2 text-sm text-[var(--text-primary)]"><input type="checkbox" checked={form.preferredBookLanguages.includes('Chinese')} disabled={!canEdit || loadingProfile} onChange={() => toggleLanguage('Chinese')} /> Chinese</label>
                </fieldset>
                <Field label="Preference notes" id="preference-notes" value={form.preferenceNotes} disabled={!canEdit || loadingProfile} onChange={value => updateField('preferenceNotes', value)} multiline />
                <label className={labelClassName} htmlFor="profile-visibility">
                  Profile visibility
                  <select id="profile-visibility" aria-label="Profile visibility" value={form.profileVisibility} disabled={!canEdit || loadingProfile} onChange={event => updateField('profileVisibility', event.target.value as ProfileFormState['profileVisibility'])} className={inputClassName}>
                    <option value="Family">Family</option>
                    <option value="Private">Private</option>
                  </select>
                </label>
                <label className="flex items-center gap-2 text-sm text-[var(--text-primary)]"><input type="checkbox" checked={form.useInFamilyRecommendations} disabled={!canEdit || loadingProfile} onChange={event => updateField('useInFamilyRecommendations', event.target.checked)} /> Use in family recommendations</label>
                {canEdit ? <Button type="submit" disabled={saving || loadingProfile}>{saving ? 'Saving…' : 'Save preferences'}</Button> : <p className="text-sm text-[var(--text-secondary)]">Only the member or a family administrator can edit this profile.</p>}
              </>
            ) : null}
            {saved ? <p role="status" className="text-sm text-[var(--text-secondary)]">Preferences saved.</p> : null}
            {error && error !== 'This profile is not available to you.' ? <p role="alert" className="text-sm text-[var(--status-alert)]">{error}</p> : null}
          </form>
        ) : null}
      </CardContent>
    </Card>
  )
}
