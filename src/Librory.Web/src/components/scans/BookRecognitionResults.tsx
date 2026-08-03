import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import type { BookRecognitionJobResponse } from '@/lib/bookRecognitionApi'

interface BookRecognitionResultsProps {
  job: BookRecognitionJobResponse
}

export function BookRecognitionResults({ job }: BookRecognitionResultsProps) {
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
          {job.candidates.length === 0 ? (
            <p className="text-sm text-[var(--text-secondary)]">No candidates were found yet.</p>
          ) : (
            job.candidates.map(candidate => (
              <div key={candidate.candidateId} className="rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-sunken)] px-4 py-3">
                <div className="flex flex-wrap items-center gap-2">
                  <h3 className="text-base font-semibold text-[var(--text-primary)]">{candidate.displayTitle}</h3>
                  <span className="rounded-full border border-[var(--border-subtle)] bg-[var(--surface-sunken)] px-2 py-0.5 text-xs font-medium text-[var(--text-secondary)]">
                    Rank {candidate.rank}
                  </span>
                </div>
                <p className="mt-1 text-sm text-[var(--text-secondary)]">
                  Evidence: {candidate.evidenceText}
                </p>
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
