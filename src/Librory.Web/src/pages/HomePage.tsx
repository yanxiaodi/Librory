import { BookOpen, ScanSearch } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { useAuthSession } from '@/auth/AuthSessionContext'

/* TODO: wire Books and Scans to API once data layer is available */
export function HomePage() {
  const { family } = useAuthSession()
  const familySize = family?.memberCount ?? 1

  return (
    <section className="grid gap-6 pb-6">
      {/* Hero */}
      <div className="pt-2 text-center">
        <BookOpen
          className="mx-auto mb-4 h-12 w-12 text-[var(--accent)]"
          strokeWidth={1.5}
        />
        <p className="font-[family-name:var(--font-display)] text-[1.7rem] font-normal italic tracking-[-0.01em] text-[var(--text-primary)]">
          Librory
        </p>
        <p className="mx-auto mt-2 max-w-[280px] text-[15px] leading-relaxed text-[var(--text-secondary)]">
          Your family's reading companion, refined for the bookshop floor.
        </p>
      </div>

      {/* Quick stats */}
      <div className="flex border-t border-b border-[var(--border-subtle)] py-5">
        <div className="flex-1 px-3 text-center">
          <p className="font-[family-name:var(--font-display)] text-[1.7rem] font-normal italic leading-none text-[var(--text-primary)]">0</p>
          <p className="mt-1 text-[9px] font-semibold uppercase tracking-[0.14em] text-[var(--text-tertiary)]">Books</p>
        </div>
        <div className="w-px self-stretch bg-[var(--border-subtle)]" />
        <div className="flex-1 px-3 text-center">
          <p className="font-[family-name:var(--font-display)] text-[1.7rem] font-normal italic leading-none text-[var(--text-primary)]">{familySize}</p>
          <p className="mt-1 text-[9px] font-semibold uppercase tracking-[0.14em] text-[var(--text-tertiary)]">Members</p>
        </div>
        <div className="w-px self-stretch bg-[var(--border-subtle)]" />
        <div className="flex-1 px-3 text-center">
          <p className="font-[family-name:var(--font-display)] text-[1.7rem] font-normal italic leading-none text-[var(--text-primary)]">0</p>
          <p className="mt-1 text-[9px] font-semibold uppercase tracking-[0.14em] text-[var(--text-tertiary)]">Scans</p>
        </div>
      </div>

      {/* Actions */}
      <div className="grid gap-3">
        <Button size="lg" asChild>
          <Link to="/app/scans">
            <ScanSearch className="h-[18px] w-[18px]" strokeWidth={1.5} />
            Scan a Shelf
          </Link>
        </Button>

        <Button size="lg" variant="outline" asChild>
          <Link to="/app/library">
            <BookOpen className="h-[18px] w-[18px]" strokeWidth={1.5} />
            Browse Library
          </Link>
        </Button>
      </div>

      {/* Recent Scans */}
      <div>
        <div className="mb-4 text-center font-[family-name:var(--font-display)] text-[1.15rem] font-normal italic text-[var(--text-primary)]">
          Recent Scans
        </div>

        <p className="py-8 text-center font-[family-name:var(--font-display)] text-sm italic text-[var(--text-tertiary)]">
          Start with your first shelf scan.
        </p>
      </div>
    </section>
  )
}
