import * as React from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import type { BookMetadataCandidateResponse, BookRecognitionJobResponse } from '@/lib/bookRecognitionApi'
import {
  purchaseScanCandidate,
  searchBookMetadata,
  type DuplicateConfirmationResponse,
  type ScanCandidateResponse,
  type ScanPurchaseResponse,
} from '@/lib/scansApi'
import type { FamilyMember } from '@/lib/familyApi'

interface BookRecognitionResultsProps {
  job: BookRecognitionJobResponse
  candidates: BookRecognitionJobResponse['candidates']
  onCandidatesChange?: (candidates: BookRecognitionJobResponse['candidates']) => void
  scanSessionId?: string
  persistedCandidates?: ScanCandidateResponse[]
  members?: FamilyMember[]
  scanTargetMemberId?: string | null
  onMetadataMatchesChange?: (candidateId: string, matches: BookMetadataCandidateResponse[]) => void
  onPurchaseComplete?: (candidateId: string, response: ScanPurchaseResponse) => void
}

type CandidatePurchaseState = 'idle' | 'searching' | 'purchasing' | 'purchased' | 'error'

export function BookRecognitionResults({
  job,
  candidates,
  onCandidatesChange,
  scanSessionId,
  persistedCandidates = [],
  members = [],
  scanTargetMemberId,
  onMetadataMatchesChange,
  onPurchaseComplete,
}: BookRecognitionResultsProps) {
  const [searchTextByCandidateId, setSearchTextByCandidateId] = React.useState<Record<string, string>>(() =>
    Object.fromEntries(candidates.map(candidate => [candidate.candidateId, candidate.displayTitle])),
  )
  const [selectedMatchByCandidateId, setSelectedMatchByCandidateId] = React.useState<Record<string, number>>({})
  const [ownerByCandidateId, setOwnerByCandidateId] = React.useState<Record<string, string>>({})
  const [purchaseTimeByCandidateId, setPurchaseTimeByCandidateId] = React.useState<Record<string, string>>({})
  const [storeByCandidateId, setStoreByCandidateId] = React.useState<Record<string, string>>({})
  const [purchaseStateByCandidateId, setPurchaseStateByCandidateId] = React.useState<Record<string, CandidatePurchaseState>>({})
  const [purchaseErrorByCandidateId, setPurchaseErrorByCandidateId] = React.useState<Record<string, string>>({})
  const [purchaseRequestIdByCandidateId, setPurchaseRequestIdByCandidateId] = React.useState<Record<string, string>>({})
  const [duplicateByCandidateId, setDuplicateByCandidateId] = React.useState<Record<string, DuplicateConfirmationResponse | undefined>>({})
  const [duplicateChoiceByCandidateId, setDuplicateChoiceByCandidateId] = React.useState<Record<string, 1 | 2 | 3>>({})

  React.useEffect(() => {
    setSearchTextByCandidateId(current => {
      const nextState: Record<string, string> = {}
      let changed = false

      for (const candidate of candidates) {
        const existingValue = current[candidate.candidateId]
        if (existingValue === undefined) {
          nextState[candidate.candidateId] = candidate.displayTitle
          changed = true
          continue
        }

        nextState[candidate.candidateId] = existingValue
      }

      if (Object.keys(current).length !== candidates.length) changed = true
      return changed ? nextState : current
    })
  }, [candidates])

  const isFailed = job.failureMessage !== null

  const removeCandidate = (candidateId: string) => {
    const nextCandidates = candidates.filter(candidate => candidate.candidateId !== candidateId)
    onCandidatesChange?.(nextCandidates)
  }

  const updateSearchText = (candidateId: string, value: string) => {
    setSearchTextByCandidateId(current => ({ ...current, [candidateId]: value }))
    onCandidatesChange?.(candidates.map(candidate =>
      candidate.candidateId === candidateId ? { ...candidate, displayTitle: value } : candidate,
    ))
  }

  const searchMetadata = async (candidateId: string) => {
    const title = searchTextByCandidateId[candidateId]?.trim()
    if (!title) return

    setPurchaseStateByCandidateId(current => ({ ...current, [candidateId]: 'searching' }))
    setPurchaseErrorByCandidateId(current => ({ ...current, [candidateId]: '' }))
    try {
      const matches = await searchBookMetadata(title)
      onMetadataMatchesChange?.(candidateId, matches)
      onCandidatesChange?.(candidates.map(candidate =>
        candidate.candidateId === candidateId ? { ...candidate, displayTitle: title, metadataMatches: matches } : candidate,
      ))
      setSelectedMatchByCandidateId(current => ({ ...current, [candidateId]: 0 }))
      setPurchaseStateByCandidateId(current => ({ ...current, [candidateId]: 'idle' }))
    } catch (error) {
      setPurchaseStateByCandidateId(current => ({ ...current, [candidateId]: 'error' }))
      setPurchaseErrorByCandidateId(current => ({ ...current, [candidateId]: error instanceof Error ? error.message : 'Metadata search failed.' }))
    }
  }

  const purchaseCandidate = async (candidate: BookRecognitionJobResponse['candidates'][number], index: number) => {
    const persisted = persistedCandidates[index]
    if (!scanSessionId || !persisted || persisted.purchaseStatus === 1) return

    const selectedMatch = candidate.metadataMatches[selectedMatchByCandidateId[candidate.candidateId] ?? 0]
    const ownerMemberId = ownerByCandidateId[candidate.candidateId] || scanTargetMemberId || members[0]?.memberId
    if (!ownerMemberId) {
      setPurchaseStateByCandidateId(current => ({ ...current, [candidate.candidateId]: 'error' }))
      setPurchaseErrorByCandidateId(current => ({ ...current, [candidate.candidateId]: 'Select a family member first.' }))
      return
    }

    const duplicateChoice = duplicateChoiceByCandidateId[candidate.candidateId] ?? 3
    const purchaseRequestId = purchaseRequestIdByCandidateId[candidate.candidateId] ?? persisted.purchaseRequestId ?? crypto.randomUUID()
    const duplicateMatch = duplicateByCandidateId[candidate.candidateId]?.matches[0]
    setPurchaseRequestIdByCandidateId(current => ({ ...current, [candidate.candidateId]: purchaseRequestId }))
    setPurchaseStateByCandidateId(current => ({ ...current, [candidate.candidateId]: 'purchasing' }))
    setPurchaseErrorByCandidateId(current => ({ ...current, [candidate.candidateId]: '' }))

    try {
      const response = await purchaseScanCandidate(scanSessionId, persisted.id, {
        purchaseRequestId,
        ownerMemberId,
        selectedMetadata: selectedMatch,
        duplicateResolution: duplicateChoice,
        duplicateStatus: duplicateByCandidateId[candidate.candidateId] ? 2 : 0,
        existingBookEditionId: duplicateChoice === 1 ? duplicateMatch?.bookEditionId : undefined,
        existingBookWorkId: duplicateChoice === 2 ? duplicateMatch?.bookWorkId : undefined,
        manualTitle: selectedMatch ? undefined : searchTextByCandidateId[candidate.candidateId],
        manualAuthor: selectedMatch?.authors[0] ?? undefined,
        purchaseStore: storeByCandidateId[candidate.candidateId] || undefined,
        purchasedAt: purchaseTimeByCandidateId[candidate.candidateId]
          ? new Date(purchaseTimeByCandidateId[candidate.candidateId]).toISOString()
          : new Date().toISOString(),
      })
      setPurchaseStateByCandidateId(current => ({ ...current, [candidate.candidateId]: 'purchased' }))
      setDuplicateByCandidateId(current => ({ ...current, [candidate.candidateId]: undefined }))
      onPurchaseComplete?.(persisted.id, response)
    } catch (error) {
      if (error && typeof error === 'object' && 'duplicate' in error) {
        const duplicate = (error as { duplicate?: DuplicateConfirmationResponse }).duplicate
        if (duplicate) {
          setDuplicateByCandidateId(current => ({ ...current, [candidate.candidateId]: duplicate }))
          setPurchaseStateByCandidateId(current => ({ ...current, [candidate.candidateId]: 'idle' }))
          return
        }
      }
      setPurchaseStateByCandidateId(current => ({ ...current, [candidate.candidateId]: 'error' }))
      setPurchaseErrorByCandidateId(current => ({ ...current, [candidate.candidateId]: error instanceof Error ? error.message : 'Purchase failed.' }))
    }
  }

  return (
    <div className="grid gap-4">
      <Card>
        <CardHeader>
          <CardTitle>Recognized candidates</CardTitle>
          <CardDescription className="text-[var(--text-secondary)]">
            Review recognition quality and metadata before buying each book into the family library.
          </CardDescription>
        </CardHeader>
        <CardContent className="grid gap-3">
          {isFailed ? (
            <div className="rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-sunken)] px-4 py-3">
              <p className="text-sm font-medium text-[var(--text-primary)]">Recognition failed</p>
              <p className="mt-1 text-sm text-[var(--text-secondary)]">{job.failureMessage}</p>
            </div>
          ) : candidates.length === 0 ? (
            <p className="text-sm text-[var(--text-secondary)]">No candidates were found yet.</p>
          ) : (
            candidates.map((candidate, index) => {
              const persisted = persistedCandidates[index]
              const selectedMatchIndex = selectedMatchByCandidateId[candidate.candidateId] ?? 0
              const purchaseState = purchaseStateByCandidateId[candidate.candidateId] ?? (persisted?.purchaseStatus === 1 ? 'purchased' : 'idle')
              const duplicate = duplicateByCandidateId[candidate.candidateId]
              const selectedMatch = candidate.metadataMatches[selectedMatchIndex] ?? candidate.metadataMatches[0]
              const defaultTime = purchaseTimeByCandidateId[candidate.candidateId] ?? new Date().toISOString().slice(0, 16)

              return (
                <div key={candidate.candidateId} className="rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-sunken)] px-4 py-3">
                  <div className="flex flex-wrap items-center gap-2">
                    <h3 className="text-base font-semibold text-[var(--text-primary)]">{candidate.displayTitle}</h3>
                    <span className="rounded-full border border-[var(--border-subtle)] px-2 py-0.5 text-xs font-medium text-[var(--text-secondary)]">
                      Recognition match #{candidate.rank}
                    </span>
                    {purchaseState === 'purchased' ? <span className="text-xs font-semibold text-[var(--accent)]">Purchased</span> : null}
                    <Button type="button" variant="outline" size="default" onClick={() => removeCandidate(candidate.candidateId)} disabled={purchaseState === 'purchased'}>
                      Remove
                    </Button>
                  </div>
                  <p className="mt-1 text-sm text-[var(--text-secondary)]">Evidence: {candidate.evidenceText}</p>
                  <label className="mt-3 grid gap-2 text-sm text-[var(--text-secondary)]" htmlFor={`search-text-${candidate.candidateId}`}>
                    Search text
                    <input
                      id={`search-text-${candidate.candidateId}`}
                      value={searchTextByCandidateId[candidate.candidateId] ?? candidate.displayTitle}
                      onChange={event => updateSearchText(candidate.candidateId, event.target.value)}
                      disabled={purchaseState === 'purchased'}
                      className="h-11 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 text-[var(--text-primary)] outline-none focus:ring-2 focus:ring-[var(--accent-subtle)]"
                    />
                  </label>
                  <Button type="button" variant="outline" className="mt-2" onClick={() => void searchMetadata(candidate.candidateId)} disabled={purchaseState === 'purchased' || purchaseState === 'searching'}>
                    {purchaseState === 'searching' ? 'Searching…' : 'Re-search metadata'}
                  </Button>
                  {candidate.metadataMatches.length > 0 ? (
                    <>
                      <ul className="mt-3 grid gap-2">
                        {candidate.metadataMatches.map(metadata => (
                          <li key={`${candidate.candidateId}-${metadata.source}-${metadata.sourceId}`} className="text-sm text-[var(--text-secondary)]">
                            <span className="font-medium text-[var(--text-primary)]">{metadata.title}</span>
                            {metadata.authors.length > 0 ? ` · ${metadata.authors.join(', ')}` : null}
                            {metadata.publishedDate ? ` · ${metadata.publishedDate}` : null}
                          </li>
                        ))}
                      </ul>
                      <label className="mt-3 grid gap-2 text-sm text-[var(--text-secondary)]" htmlFor={`metadata-match-${candidate.candidateId}`}>
                        Metadata match
                        <select
                          id={`metadata-match-${candidate.candidateId}`}
                          value={selectedMatchIndex}
                          onChange={event => setSelectedMatchByCandidateId(current => ({ ...current, [candidate.candidateId]: Number(event.target.value) }))}
                          disabled={purchaseState === 'purchased'}
                          className="h-11 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 text-[var(--text-primary)]"
                        >
                          {candidate.metadataMatches.map((metadata, metadataIndex) => (
                            <option key={`${candidate.candidateId}-option-${metadata.source}-${metadata.sourceId}`} value={metadataIndex}>
                              {metadata.title}{metadata.authors.length > 0 ? ` · ${metadata.authors.join(', ')}` : ''}
                            </option>
                          ))}
                        </select>
                      </label>
                    </>
                  ) : null}
                  {selectedMatch ? <p className="mt-2 text-sm text-[var(--text-secondary)]">Selected: {selectedMatch.title}{selectedMatch.publishedDate ? ` · ${selectedMatch.publishedDate}` : ''}</p> : null}

                  {purchaseState !== 'purchased' && scanSessionId ? (
                    <div className="mt-4 grid gap-2 border-t border-[var(--border-subtle)] pt-3">
                      <label className="grid gap-2 text-sm text-[var(--text-secondary)]" htmlFor={`purchase-owner-${candidate.candidateId}`}>
                        Buy for
                        <select
                          id={`purchase-owner-${candidate.candidateId}`}
                          value={ownerByCandidateId[candidate.candidateId] ?? scanTargetMemberId ?? members[0]?.memberId ?? ''}
                          onChange={event => setOwnerByCandidateId(current => ({ ...current, [candidate.candidateId]: event.target.value }))}
                          className="h-11 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 text-[var(--text-primary)]"
                        >
                          {members.map(member => <option key={member.memberId} value={member.memberId}>{member.displayName}{member.isActive ? '' : ' (inactive)'}</option>)}
                        </select>
                      </label>
                      <label className="grid gap-2 text-sm text-[var(--text-secondary)]" htmlFor={`purchase-time-${candidate.candidateId}`}>
                        Purchase time
                        <input
                          id={`purchase-time-${candidate.candidateId}`}
                          type="datetime-local"
                          value={defaultTime}
                          onChange={event => setPurchaseTimeByCandidateId(current => ({ ...current, [candidate.candidateId]: event.target.value }))}
                          className="h-11 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 text-[var(--text-primary)]"
                        />
                      </label>
                      <label className="grid gap-2 text-sm text-[var(--text-secondary)]" htmlFor={`purchase-store-${candidate.candidateId}`}>
                        Store (optional)
                        <input
                          id={`purchase-store-${candidate.candidateId}`}
                          value={storeByCandidateId[candidate.candidateId] ?? ''}
                          onChange={event => setStoreByCandidateId(current => ({ ...current, [candidate.candidateId]: event.target.value }))}
                          className="h-11 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 text-[var(--text-primary)]"
                        />
                      </label>
                      {duplicate ? (
                        <div className="grid gap-2 rounded-[var(--radius-md)] border border-[var(--accent)]/40 bg-[var(--accent)]/5 p-3 text-sm text-[var(--text-secondary)]">
                          <p className="font-medium text-[var(--text-primary)]">Possible duplicate</p>
                          <p>{duplicate.followUpHint ?? duplicate.message}</p>
                          <select
                            aria-label={`Duplicate resolution for ${candidate.displayTitle}`}
                            value={duplicateChoiceByCandidateId[candidate.candidateId] ?? 3}
                            onChange={event => setDuplicateChoiceByCandidateId(current => ({ ...current, [candidate.candidateId]: Number(event.target.value) as 1 | 2 | 3 }))}
                            className="h-11 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 text-[var(--text-primary)]"
                          >
                            <option value={1}>Same existing edition</option>
                            <option value={2}>Same work, new edition</option>
                            <option value={3}>Not the same book, new work</option>
                          </select>
                        </div>
                      ) : null}
                      <Button type="button" onClick={() => void purchaseCandidate(candidate, index)} disabled={purchaseState === 'purchasing'}>
                        {purchaseState === 'purchasing' ? 'Saving purchase…' : duplicate ? 'Confirm duplicate and buy' : 'Buy this book'}
                      </Button>
                      {purchaseErrorByCandidateId[candidate.candidateId] ? <p className="text-sm text-[var(--text-secondary)]">{purchaseErrorByCandidateId[candidate.candidateId]}</p> : null}
                    </div>
                  ) : null}
                </div>
              )
            })
          )}
        </CardContent>
      </Card>

      {job.warnings.length > 0 ? (
        <Card>
          <CardHeader><CardTitle>Processing warnings</CardTitle></CardHeader>
          <CardContent className="grid gap-2 text-sm text-[var(--text-secondary)]">{job.warnings.map(warning => <p key={warning}>{warning}</p>)}</CardContent>
        </Card>
      ) : null}
    </div>
  )
}
