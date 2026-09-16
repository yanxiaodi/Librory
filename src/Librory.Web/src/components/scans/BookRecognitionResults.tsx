import * as React from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import type { BookMetadataCandidateResponse, BookRecognitionJobResponse } from '@/lib/bookRecognitionApi'
import {
  purchaseScanCandidate,
  searchBookMetadata,
  discardScanCandidate,
  updateBookEditionVersion,
  type DuplicateConfirmationResponse,
  type ScanCandidateResponse,
  type ScanPurchaseResponse,
} from '@/lib/scansApi'
import type { FamilyMember } from '@/lib/familyApi'

interface BookRecognitionResultsProps {
  job: BookRecognitionJobResponse
  candidates: BookRecognitionJobResponse['candidates']
  onCandidatesChange?: React.Dispatch<React.SetStateAction<BookRecognitionJobResponse['candidates']>>
  scanSessionId?: string
  persistedCandidates?: ScanCandidateResponse[]
  members?: FamilyMember[]
  scanTargetMemberId?: string | null
  persistencePending?: boolean
  onMetadataMatchesChange?: (candidateId: string, matches: BookMetadataCandidateResponse[]) => Promise<void>
  onPurchaseComplete?: (candidateId: string, response: ScanPurchaseResponse) => void
}

type CandidatePurchaseState = 'idle' | 'searching' | 'purchasing' | 'purchased' | 'error'

function localDateTimeValue(date = new Date()): string {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 16)
}

export function BookRecognitionResults({
  job,
  candidates,
  onCandidatesChange,
  scanSessionId,
  persistedCandidates = [],
  members = [],
  scanTargetMemberId,
  persistencePending = false,
  onMetadataMatchesChange,
  onPurchaseComplete,
}: BookRecognitionResultsProps) {
  const [searchTextByCandidateId, setSearchTextByCandidateId] = React.useState<Record<string, string>>(() =>
    Object.fromEntries(candidates.map(candidate => [candidate.candidateId, candidate.displayTitle])),
  )
  const [metadataSearchRequiredByCandidateId, setMetadataSearchRequiredByCandidateId] = React.useState<Record<string, boolean>>({})
  const metadataSearchRequestIdByCandidateId = React.useRef<Record<string, number>>({})
  const [selectedMatchByCandidateId, setSelectedMatchByCandidateId] = React.useState<Record<string, number>>({})
  const [ownerByCandidateId, setOwnerByCandidateId] = React.useState<Record<string, string>>({})
  const [purchaseTimeByCandidateId, setPurchaseTimeByCandidateId] = React.useState<Record<string, string>>({})
  const [storeByCandidateId, setStoreByCandidateId] = React.useState<Record<string, string>>({})
  const [conditionByCandidateId, setConditionByCandidateId] = React.useState<Record<string, string>>({})
  const [priceByCandidateId, setPriceByCandidateId] = React.useState<Record<string, string>>({})
  const [shelfLocationByCandidateId, setShelfLocationByCandidateId] = React.useState<Record<string, string>>({})
  const [intakeNotesByCandidateId, setIntakeNotesByCandidateId] = React.useState<Record<string, string>>({})
  const [purchaseStateByCandidateId, setPurchaseStateByCandidateId] = React.useState<Record<string, CandidatePurchaseState>>({})
  const [purchaseErrorByCandidateId, setPurchaseErrorByCandidateId] = React.useState<Record<string, string>>({})
  const [purchaseRequestIdByCandidateId, setPurchaseRequestIdByCandidateId] = React.useState<Record<string, string>>({})
  const [duplicateByCandidateId, setDuplicateByCandidateId] = React.useState<Record<string, DuplicateConfirmationResponse | undefined>>({})
  const [duplicateChoiceByCandidateId, setDuplicateChoiceByCandidateId] = React.useState<Record<string, 1 | 2 | 3>>({})
  const [duplicateMatchIndexByCandidateId, setDuplicateMatchIndexByCandidateId] = React.useState<Record<string, number>>({})
  const [purchaseResponseByCandidateId, setPurchaseResponseByCandidateId] = React.useState<Record<string, ScanPurchaseResponse | undefined>>({})
  const [versionByCandidateId, setVersionByCandidateId] = React.useState<Record<string, { isbn: string; format: string; publicationYear: string }>>({})
  const [versionStateByCandidateId, setVersionStateByCandidateId] = React.useState<Record<string, 'idle' | 'saving' | 'saved' | 'error'>>({})
  const [versionErrorByCandidateId, setVersionErrorByCandidateId] = React.useState<Record<string, string>>({})

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

  const removeCandidate = async (candidateId: string) => {
    if (persistencePending) return

    const persisted = persistedCandidates.find(item => item.id === candidateId)
    if (scanSessionId && persisted) {
      try {
        await discardScanCandidate(scanSessionId, persisted.id)
      } catch (error) {
        setPurchaseErrorByCandidateId(current => ({
          ...current,
          [candidateId]: error instanceof Error ? error.message : 'Candidate discard failed.',
        }))
        return
      }
    }

    onCandidatesChange?.(current => current.filter(candidate => candidate.candidateId !== candidateId))
  }

  const updateSearchText = (candidateId: string, value: string) => {
    if (persistencePending) return
    metadataSearchRequestIdByCandidateId.current[candidateId] = (metadataSearchRequestIdByCandidateId.current[candidateId] ?? 0) + 1
    setSearchTextByCandidateId(current => ({ ...current, [candidateId]: value }))
    setMetadataSearchRequiredByCandidateId(current => ({ ...current, [candidateId]: true }))
    setPurchaseStateByCandidateId(current => ({ ...current, [candidateId]: 'idle' }))
    setSelectedMatchByCandidateId(current => ({ ...current, [candidateId]: 0 }))
    setDuplicateByCandidateId(current => ({ ...current, [candidateId]: undefined }))
    setDuplicateChoiceByCandidateId(current => {
      const next = { ...current }
      delete next[candidateId]
      return next
    })
    setDuplicateMatchIndexByCandidateId(current => {
      const next = { ...current }
      delete next[candidateId]
      return next
    })
    onCandidatesChange?.(current => current.map(candidate =>
      candidate.candidateId === candidateId
        ? { ...candidate, displayTitle: value, metadataMatches: [] }
        : candidate,
    ))
  }

  const searchMetadata = async (candidateId: string) => {
    if (persistencePending) return
    const title = searchTextByCandidateId[candidateId]?.trim()
    if (!title) return

    const requestId = (metadataSearchRequestIdByCandidateId.current[candidateId] ?? 0) + 1
    metadataSearchRequestIdByCandidateId.current[candidateId] = requestId
    setPurchaseStateByCandidateId(current => ({ ...current, [candidateId]: 'searching' }))
    setPurchaseErrorByCandidateId(current => ({ ...current, [candidateId]: '' }))
    setDuplicateByCandidateId(current => ({ ...current, [candidateId]: undefined }))
    setDuplicateChoiceByCandidateId(current => {
      const next = { ...current }
      delete next[candidateId]
      return next
    })
    setDuplicateMatchIndexByCandidateId(current => ({ ...current, [candidateId]: 0 }))
    try {
      const matches = await searchBookMetadata(title)
      if (metadataSearchRequestIdByCandidateId.current[candidateId] !== requestId) return
      await onMetadataMatchesChange?.(candidateId, matches)
      if (metadataSearchRequestIdByCandidateId.current[candidateId] !== requestId) return
      onCandidatesChange?.(current => current.map(candidate =>
        candidate.candidateId === candidateId ? { ...candidate, displayTitle: title, metadataMatches: matches } : candidate,
      ))
      setSelectedMatchByCandidateId(current => ({ ...current, [candidateId]: 0 }))
      setMetadataSearchRequiredByCandidateId(current => ({ ...current, [candidateId]: false }))
      setPurchaseStateByCandidateId(current => ({ ...current, [candidateId]: 'idle' }))
    } catch (error) {
      setPurchaseStateByCandidateId(current => ({ ...current, [candidateId]: 'error' }))
      setPurchaseErrorByCandidateId(current => ({ ...current, [candidateId]: error instanceof Error ? error.message : 'Metadata search failed.' }))
    }
  }

  const purchaseCandidate = async (candidate: BookRecognitionJobResponse['candidates'][number]) => {
    if (persistencePending) return
    const persisted = persistedCandidates.find(item => item.id === candidate.candidateId)
    if (!scanSessionId || !persisted || persisted.purchaseStatus === 1) return
    if (metadataSearchRequiredByCandidateId[candidate.candidateId]) {
      setPurchaseStateByCandidateId(current => ({ ...current, [candidate.candidateId]: 'error' }))
      setPurchaseErrorByCandidateId(current => ({ ...current, [candidate.candidateId]: 'Search metadata again before buying this edited title.' }))
      return
    }

    const selectedMatch = candidate.metadataMatches[selectedMatchByCandidateId[candidate.candidateId] ?? 0]
    const ownerMemberId = ownerByCandidateId[candidate.candidateId] || scanTargetMemberId || members[0]?.memberId
    if (!ownerMemberId) {
      setPurchaseStateByCandidateId(current => ({ ...current, [candidate.candidateId]: 'error' }))
      setPurchaseErrorByCandidateId(current => ({ ...current, [candidate.candidateId]: 'Select a family member first.' }))
      return
    }

    const duplicateChoice = duplicateChoiceByCandidateId[candidate.candidateId] ?? 3
    const purchaseRequestId = purchaseRequestIdByCandidateId[candidate.candidateId] ?? persisted.purchaseRequestId ?? crypto.randomUUID()
    const duplicate = duplicateByCandidateId[candidate.candidateId]
    const duplicateMatch = duplicate?.matches[duplicateMatchIndexByCandidateId[candidate.candidateId] ?? 0]
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
        condition: conditionByCandidateId[candidate.candidateId] || undefined,
        purchasePrice: priceByCandidateId[candidate.candidateId]?.trim()
          ? Number(priceByCandidateId[candidate.candidateId])
          : undefined,
        shelfLocation: shelfLocationByCandidateId[candidate.candidateId] || undefined,
        purchasedAt: purchaseTimeByCandidateId[candidate.candidateId]
          ? new Date(purchaseTimeByCandidateId[candidate.candidateId]).toISOString()
          : new Date().toISOString(),
        intakeNotes: intakeNotesByCandidateId[candidate.candidateId] || undefined,
      })
      setPurchaseStateByCandidateId(current => ({ ...current, [candidate.candidateId]: 'purchased' }))
      setDuplicateByCandidateId(current => ({ ...current, [candidate.candidateId]: undefined }))
      setPurchaseResponseByCandidateId(current => ({ ...current, [candidate.candidateId]: response }))
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

  const confirmVersion = async (candidateId: string, purchaseResponse: ScanPurchaseResponse) => {
    const edition = purchaseResponse.work.editions.find(item => item.bookEditionId === purchaseResponse.bookEditionId)
    const version = versionByCandidateId[candidateId] ?? {
      isbn: edition?.isbn ?? '',
      format: edition?.format ?? '',
      publicationYear: edition?.publicationYear?.toString() ?? '',
    }
    if (!version.isbn.trim() && !version.format.trim() && !version.publicationYear.trim()) {
      setVersionStateByCandidateId(current => ({ ...current, [candidateId]: 'error' }))
      setVersionErrorByCandidateId(current => ({ ...current, [candidateId]: 'Add an ISBN, format, or publication year first.' }))
      return
    }

    setVersionStateByCandidateId(current => ({ ...current, [candidateId]: 'saving' }))
    setVersionErrorByCandidateId(current => ({ ...current, [candidateId]: '' }))
    try {
      const updatedEdition = await updateBookEditionVersion(purchaseResponse.bookEditionId, {
        isbn: version.isbn.trim() || undefined,
        format: version.format.trim() || undefined,
        publicationYear: version.publicationYear.trim() ? Number(version.publicationYear) : undefined,
      })
      setPurchaseResponseByCandidateId(current => ({
        ...current,
        [candidateId]: {
          ...purchaseResponse,
          isProvisional: updatedEdition.isProvisional,
          work: {
            ...purchaseResponse.work,
            editions: purchaseResponse.work.editions.map(item => item.bookEditionId === updatedEdition.bookEditionId
              ? { ...item, ...updatedEdition }
              : item),
          },
        },
      }))
      setVersionStateByCandidateId(current => ({ ...current, [candidateId]: 'saved' }))
    } catch (error) {
      setVersionStateByCandidateId(current => ({ ...current, [candidateId]: 'error' }))
      setVersionErrorByCandidateId(current => ({ ...current, [candidateId]: error instanceof Error ? error.message : 'Version confirmation failed.' }))
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
            candidates.map(candidate => {
              const persisted = persistedCandidates.find(item => item.id === candidate.candidateId)
              const selectedMatchIndex = selectedMatchByCandidateId[candidate.candidateId] ?? 0
              const purchaseState = purchaseStateByCandidateId[candidate.candidateId] ?? (persisted?.purchaseStatus === 1 ? 'purchased' : 'idle')
              const duplicate = duplicateByCandidateId[candidate.candidateId]
              const selectedMatch = candidate.metadataMatches[selectedMatchIndex] ?? candidate.metadataMatches[0]
              const metadataSearchRequired = metadataSearchRequiredByCandidateId[candidate.candidateId] === true
              const defaultTime = purchaseTimeByCandidateId[candidate.candidateId] ?? localDateTimeValue()
              const purchaseResponse = purchaseResponseByCandidateId[candidate.candidateId]
                ?? (persisted?.purchase ? { ...persisted.purchase, isReplay: true } : undefined)
              const purchasedEdition = purchaseResponse?.work.editions.find(item => item.bookEditionId === purchaseResponse.bookEditionId)
              const version = versionByCandidateId[candidate.candidateId] ?? {
                isbn: purchasedEdition?.isbn ?? '',
                format: purchasedEdition?.format ?? '',
                publicationYear: purchasedEdition?.publicationYear?.toString() ?? '',
              }
              const versionState = versionStateByCandidateId[candidate.candidateId] ?? 'idle'

              return (
                <div key={candidate.candidateId} className="rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-sunken)] px-4 py-3">
                  <div className="flex flex-wrap items-center gap-2">
                    <h3 className="text-base font-semibold text-[var(--text-primary)]">{candidate.displayTitle}</h3>
                    <span className="rounded-full border border-[var(--border-subtle)] px-2 py-0.5 text-xs font-medium text-[var(--text-secondary)]">
                      {candidate.rank > 0 ? `Recognition rank: ${candidate.rank}` : 'Recognition rank unavailable'}
                    </span>
                    {purchaseState === 'purchased' ? <span className="text-xs font-semibold text-[var(--accent)]">Purchased</span> : null}
                    <Button type="button" variant="outline" size="default" onClick={() => removeCandidate(candidate.candidateId)} disabled={persistencePending || purchaseState === 'purchased'}>
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
                      disabled={persistencePending || purchaseState === 'purchased' || purchaseState === 'searching'}
                      className="h-11 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 text-[var(--text-primary)] outline-none focus:ring-2 focus:ring-[var(--accent-subtle)]"
                    />
                  </label>
                  <Button type="button" variant="outline" className="mt-2" onClick={() => void searchMetadata(candidate.candidateId)} disabled={persistencePending || purchaseState === 'purchased' || purchaseState === 'searching'}>
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
                      onChange={event => {
                        setSelectedMatchByCandidateId(current => ({ ...current, [candidate.candidateId]: Number(event.target.value) }))
                        setDuplicateByCandidateId(current => ({ ...current, [candidate.candidateId]: undefined }))
                        setDuplicateChoiceByCandidateId(current => {
                          const next = { ...current }
                          delete next[candidate.candidateId]
                          return next
                        })
                        setDuplicateMatchIndexByCandidateId(current => {
                          const next = { ...current }
                          delete next[candidate.candidateId]
                          return next
                        })
                      }}
                          disabled={persistencePending || purchaseState === 'purchased'}
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

                  {scanSessionId ? (
                    <div className="mt-4 grid gap-2 border-t border-[var(--border-subtle)] pt-3">
                      {purchaseState !== 'purchased' ? (
                        <>
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
                      <label className="grid gap-2 text-sm text-[var(--text-secondary)]" htmlFor={`purchase-condition-${candidate.candidateId}`}>
                        Condition (optional)
                        <input
                          id={`purchase-condition-${candidate.candidateId}`}
                          value={conditionByCandidateId[candidate.candidateId] ?? ''}
                          onChange={event => setConditionByCandidateId(current => ({ ...current, [candidate.candidateId]: event.target.value }))}
                          className="h-11 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 text-[var(--text-primary)]"
                        />
                      </label>
                      <label className="grid gap-2 text-sm text-[var(--text-secondary)]" htmlFor={`purchase-price-${candidate.candidateId}`}>
                        Price (optional)
                        <input
                          id={`purchase-price-${candidate.candidateId}`}
                          type="number"
                          min="0"
                          step="0.01"
                          value={priceByCandidateId[candidate.candidateId] ?? ''}
                          onChange={event => setPriceByCandidateId(current => ({ ...current, [candidate.candidateId]: event.target.value }))}
                          className="h-11 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 text-[var(--text-primary)]"
                        />
                      </label>
                      <label className="grid gap-2 text-sm text-[var(--text-secondary)]" htmlFor={`purchase-shelf-location-${candidate.candidateId}`}>
                        Shelf location (optional)
                        <input
                          id={`purchase-shelf-location-${candidate.candidateId}`}
                          value={shelfLocationByCandidateId[candidate.candidateId] ?? ''}
                          onChange={event => setShelfLocationByCandidateId(current => ({ ...current, [candidate.candidateId]: event.target.value }))}
                          className="h-11 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 text-[var(--text-primary)]"
                        />
                      </label>
                      <label className="grid gap-2 text-sm text-[var(--text-secondary)]" htmlFor={`purchase-intake-notes-${candidate.candidateId}`}>
                        Intake notes (optional)
                        <textarea
                          id={`purchase-intake-notes-${candidate.candidateId}`}
                          value={intakeNotesByCandidateId[candidate.candidateId] ?? ''}
                          onChange={event => setIntakeNotesByCandidateId(current => ({ ...current, [candidate.candidateId]: event.target.value }))}
                          rows={3}
                          className="rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 py-2 text-[var(--text-primary)]"
                        />
                      </label>
                      {duplicate ? (
                        <div className="grid gap-2 rounded-[var(--radius-md)] border border-[var(--accent)]/40 bg-[var(--accent)]/5 p-3 text-sm text-[var(--text-secondary)]">
                          <p className="font-medium text-[var(--text-primary)]">Possible duplicate</p>
                          <p>{duplicate.followUpHint ?? duplicate.message}</p>
                          <label className="grid gap-2" htmlFor={`duplicate-match-${candidate.candidateId}`}>
                            Matching family copy
                            <select
                              id={`duplicate-match-${candidate.candidateId}`}
                              value={duplicateMatchIndexByCandidateId[candidate.candidateId] ?? 0}
                              onChange={event => setDuplicateMatchIndexByCandidateId(current => ({ ...current, [candidate.candidateId]: Number(event.target.value) }))}
                              className="h-11 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 text-[var(--text-primary)]"
                            >
                              {duplicate.matches.map((match, matchIndex) => (
                                <option key={`${candidate.candidateId}-duplicate-${match.bookCopyId}`} value={matchIndex}>
                                  {match.title}{match.format ? ` · ${match.format}` : ''}{match.publicationYear ? ` · ${match.publicationYear}` : ''}
                                </option>
                              ))}
                            </select>
                          </label>
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
                      <Button type="button" onClick={() => void purchaseCandidate(candidate)} disabled={persistencePending || purchaseState === 'purchasing' || metadataSearchRequired}>
                        {purchaseState === 'purchasing' ? 'Saving purchase…' : duplicate ? 'Confirm duplicate and buy' : 'Buy this book'}
                      </Button>
                        </>
                      ) : null}
                      {purchaseResponse ? (
                        <div className="grid gap-2 text-sm text-[var(--text-secondary)]">
                          <p>
                          Added copy {purchaseResponse.copy.bookCopyId} for {members.find(member => member.memberId === purchaseResponse.copy.memberId)?.displayName ?? purchaseResponse.copy.memberId}.
                          {purchaseResponse.isProvisional ? ' Version details are still provisional.' : ' Edition details confirmed.'}
                          </p>
                          {purchaseResponse.isProvisional ? (
                            <div className="grid gap-2 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] p-3">
                              <p className="font-medium text-[var(--text-primary)]">Confirm version details</p>
                              <input
                                aria-label={`ISBN for ${candidate.displayTitle}`}
                                placeholder="ISBN"
                                value={version.isbn}
                                onChange={event => setVersionByCandidateId(current => ({ ...current, [candidate.candidateId]: { ...version, isbn: event.target.value } }))}
                                className="h-11 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 text-[var(--text-primary)]"
                              />
                              <input
                                aria-label={`Format for ${candidate.displayTitle}`}
                                placeholder="Format"
                                value={version.format}
                                onChange={event => setVersionByCandidateId(current => ({ ...current, [candidate.candidateId]: { ...version, format: event.target.value } }))}
                                className="h-11 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 text-[var(--text-primary)]"
                              />
                              <input
                                aria-label={`Publication year for ${candidate.displayTitle}`}
                                placeholder="Publication year"
                                inputMode="numeric"
                                value={version.publicationYear}
                                onChange={event => setVersionByCandidateId(current => ({ ...current, [candidate.candidateId]: { ...version, publicationYear: event.target.value } }))}
                                className="h-11 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 text-[var(--text-primary)]"
                              />
                              <Button type="button" variant="outline" onClick={() => void confirmVersion(candidate.candidateId, purchaseResponse)} disabled={versionState === 'saving'}>
                                {versionState === 'saving' ? 'Confirming version…' : 'Confirm version'}
                              </Button>
                              {versionErrorByCandidateId[candidate.candidateId] ? <p>{versionErrorByCandidateId[candidate.candidateId]}</p> : null}
                            </div>
                          ) : null}
                        </div>
                      ) : null}
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
