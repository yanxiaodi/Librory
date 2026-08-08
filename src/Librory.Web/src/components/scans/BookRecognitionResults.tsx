import * as React from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import type { BookRecognitionJobResponse } from '@/lib/bookRecognitionApi'

interface BookRecognitionResultsProps {
  job: BookRecognitionJobResponse
  candidates: BookRecognitionJobResponse['candidates']
  onCandidatesChange?: (candidates: BookRecognitionJobResponse['candidates']) => void
}

export function BookRecognitionResults({ job, candidates, onCandidatesChange }: BookRecognitionResultsProps) {
  const [searchTextByCandidateId, setSearchTextByCandidateId] = React.useState<Record<string, string>>(() =>
    Object.fromEntries(candidates.map(candidate => [candidate.candidateId, candidate.displayTitle])),
  )

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

      if (Object.keys(current).length !== candidates.length) {
        changed = true
      }

      return changed ? nextState : current
    })
  }, [candidates])

  const isFailed = job.failureMessage !== null

  const removeCandidate = (candidateId: string) => {
    const nextCandidates = candidates.filter(candidate => candidate.candidateId !== candidateId)
    onCandidatesChange?.(nextCandidates)
  }

  const updateSearchText = (candidateId: string, value: string) => {
    setSearchTextByCandidateId(current => ({
      ...current,
      [candidateId]: value,
    }))

    const nextCandidates = candidates.map(candidate =>
      candidate.candidateId === candidateId ? { ...candidate, displayTitle: value } : candidate,
    )
    onCandidatesChange?.(nextCandidates)
  }

  return (
    <div className="grid gap-4">
      <Card>
        <CardHeader>
          <CardTitle>Recognized candidates</CardTitle>
          <CardDescription className="text-[var(--text-secondary)]">
            Review the strongest matches before moving on to metadata correction or recommendation.
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
            candidates.map(candidate => (
              <div key={candidate.candidateId} className="rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-sunken)] px-4 py-3">
                <div className="flex flex-wrap items-center gap-2">
                  <h3 className="text-base font-semibold text-[var(--text-primary)]">{candidate.displayTitle}</h3>
                  <span className="rounded-full border border-[var(--border-subtle)] bg-[var(--surface-sunken)] px-2 py-0.5 text-xs font-medium text-[var(--text-secondary)]">
                    Rank {candidate.rank}
                  </span>
                  <Button type="button" variant="outline" size="default" onClick={() => removeCandidate(candidate.candidateId)}>
                    Remove
                  </Button>
                </div>
                <p className="mt-1 text-sm text-[var(--text-secondary)]">
                  Evidence: {candidate.evidenceText}
                </p>
                <label className="mt-3 grid gap-2 text-sm text-[var(--text-secondary)]" htmlFor={`search-text-${candidate.candidateId}`}>
                  Search text
                  <input
                    id={`search-text-${candidate.candidateId}`}
                    value={searchTextByCandidateId[candidate.candidateId] ?? candidate.displayTitle}
                    onChange={event => updateSearchText(candidate.candidateId, event.target.value)}
                    className="h-11 rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-3 text-[var(--text-primary)] outline-none focus:ring-2 focus:ring-[var(--accent-subtle)]"
                  />
                </label>
                {candidate.metadataMatches.length > 0 ? (
                  <ul className="mt-3 grid gap-2">
                    {candidate.metadataMatches.map(metadata => (
                      <li key={`${candidate.candidateId}-${metadata.source}-${metadata.sourceId}`} className="text-sm text-[var(--text-secondary)]">
                        <span className="font-medium text-[var(--text-primary)]">{metadata.title}</span>
                        {metadata.subtitle ? ` · ${metadata.subtitle}` : null}
                        {metadata.authors.length > 0 ? ` · ${metadata.authors.join(', ')}` : null}
                        {metadata.publishedDate ? ` · ${metadata.publishedDate}` : null}
                      </li>
                    ))}
                  </ul>
                ) : null}
              </div>
            ))
          )}
        </CardContent>
      </Card>

      {job.warnings.length > 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>Processing warnings</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-2 text-sm text-[var(--text-secondary)]">
            {job.warnings.map(warning => (
              <p key={warning}>{warning}</p>
            ))}
          </CardContent>
        </Card>
      ) : null}
    </div>
  )
}
