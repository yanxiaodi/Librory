import { useAuthSession } from '@/auth/AuthSessionContext'
import { HomeSummaryStrip } from '@/components/home/HomeSummaryStrip'
import { PrimaryScanAction } from '@/components/home/PrimaryScanAction'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export function HomePage() {
  const { family } = useAuthSession()
  const familySize = family?.memberCount ?? 1

  return (
    <section className="grid gap-5">
      <div className="rounded-[28px] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-5 py-5 shadow-none">
        <p className="text-[11px] font-semibold uppercase tracking-[0.24em] text-[var(--text-tertiary)]">Today</p>
        <h2 className="mt-2 font-[family-name:var(--font-display)] text-[1.7rem] font-normal italic tracking-[-0.01em] text-[var(--text-primary)]">
          Today&apos;s shelf
        </h2>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-[var(--text-secondary)]">
          Start with the camera action. Everything else stays small enough to scan at a glance.
        </p>
      </div>

      <PrimaryScanAction />

      <HomeSummaryStrip summary={{ bookCount: 0, scanCount: 0, familySize }} />

        <Card className="border-[var(--border-subtle)] bg-[var(--surface-elevated)] shadow-none">
          <CardHeader className="p-4 pb-0">
            <CardTitle className="font-[family-name:var(--font-display)] text-[1.15rem] font-normal italic text-[var(--text-primary)]">
            Scan history
            </CardTitle>
          </CardHeader>
        <CardContent className="grid gap-3 p-4">
          <div className="rounded-[18px] border border-dashed border-[var(--border-subtle)] bg-[var(--accent-muted)] px-4 py-4 text-sm leading-6 text-[var(--text-secondary)]">
            No scans yet. The first shelf photo you take will appear here.
          </div>
        </CardContent>
      </Card>
    </section>
  )
}
