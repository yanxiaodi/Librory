import { Card, CardContent } from '@/components/ui/card'
import { cn } from '@/lib/utils'

export type HomeSummary = {
  bookCount: number
  scanCount: number
  familySize: number
}

const summaryItems = [
  { key: 'bookCount', label: 'Books saved' },
  { key: 'scanCount', label: 'Recent scans' },
  { key: 'familySize', label: 'Family size' },
] as const

export function HomeSummaryStrip({ summary }: { summary: HomeSummary }) {
  return (
    <div className="grid gap-3 sm:grid-cols-3">
      {summaryItems.map(({ key, label }) => (
        <Card key={key} className="border-[var(--border-subtle)] bg-[var(--surface-elevated)] shadow-none">
          <CardContent className="flex items-start justify-between gap-3 p-4">
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-[var(--text-tertiary)]">{label}</p>
              <p className="mt-2 font-[family-name:var(--font-display)] text-[2rem] font-normal italic leading-none text-[var(--text-primary)]">
                {summary[key]}
              </p>
            </div>
            <div className={cn('mt-1 h-2.5 w-2.5 rounded-full', key === 'scanCount' ? 'bg-[var(--accent)]' : 'bg-[var(--status-recommend)]')} />
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
