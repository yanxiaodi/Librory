import { Link } from 'react-router-dom'
import { ArrowRight, BookOpen, ScanSearch, Sparkles } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { cn } from '@/lib/utils'

const highlights = [
  {
    icon: ScanSearch,
    title: 'Scan fast at the shelf',
    description: 'Open the camera flow first, then decide whether the book is worth the buy.',
  },
  {
    icon: BookOpen,
    title: 'Keep a shared family view',
    description: 'Every saved book lives in one family space, even when that family has only one person.',
  },
  {
    icon: Sparkles,
    title: 'Start with the useful signals',
    description: 'See only the stats that help you move quickly: books saved, recent scans, and family size.',
  },
] as const

function ScreenshotCard({
  title,
  subtitle,
  body,
  accentClassName,
}: {
  title: string
  subtitle: string
  body: string
  accentClassName: string
}) {
  return (
    <div className="overflow-hidden rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] shadow-[0_12px_32px_rgba(58,48,42,0.05)]">
      <div className={cn('h-3', accentClassName)} />
      <div className="p-4">
        <p className="text-[10px] font-semibold uppercase tracking-[0.24em] text-[var(--text-tertiary)]">{subtitle}</p>
        <h3 className="mt-2 font-[family-name:var(--font-display)] text-[1.15rem] font-normal italic text-[var(--text-primary)]">
          {title}
        </h3>
        <p className="mt-2 text-sm leading-6 text-[var(--text-secondary)]">{body}</p>
      </div>
    </div>
  )
}

export function LandingPage() {
  return (
    <main className="min-h-screen bg-[var(--page-bg)] px-5 py-6 text-[var(--text-primary)]">
      <div className="mx-auto flex max-w-6xl flex-col gap-8">
        <header className="flex items-center justify-between">
          <div>
            <h1 className="text-[11px] font-semibold uppercase tracking-[0.24em] text-[var(--text-tertiary)]">Librory</h1>
            <p className="mt-1 text-sm text-[var(--text-secondary)]">Shelf scanning for the bookshop floor.</p>
          </div>
          <Button asChild variant="outline">
            <Link to="/login">Sign in</Link>
          </Button>
        </header>

        <section className="grid gap-8 lg:grid-cols-[1.15fr_0.85fr] lg:items-center">
          <div className="max-w-2xl">
            <p className="text-[11px] font-semibold uppercase tracking-[0.3em] text-[var(--accent)]">Scan before you buy</p>
            <h2 className="mt-3 max-w-xl font-[family-name:var(--font-display)] text-[clamp(2.8rem,5.6vw,4.8rem)] font-normal italic tracking-[-0.03em] text-[var(--text-primary)]">
              A private book companion for the shelf.
            </h2>
            <p className="mt-5 max-w-2xl text-[1rem] leading-7 text-[var(--text-secondary)] sm:text-[1.05rem]">
              Librory helps you scan a bookstore shelf, see the strongest matches, and decide quickly without exposing
              private family data before sign-in.
            </p>

            <div className="mt-8 flex flex-col gap-3 sm:flex-row">
              <Button asChild size="lg">
                <Link to="/login">
                  Sign in to Librory
                  <ArrowRight className="h-4 w-4" />
                </Link>
              </Button>
              <Button asChild size="lg" variant="outline">
                <a href="#screenshots">See the flow</a>
              </Button>
            </div>
          </div>

          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-1">
            <ScreenshotCard
              subtitle="Home preview"
              title="Open the shelf-first home"
              body="Keep the scan button up top and the status strip small enough to understand at a glance."
              accentClassName="bg-[linear-gradient(90deg,_var(--accent),_rgba(181,120,115,0.35))]"
            />
            <ScreenshotCard
              subtitle="Scan preview"
              title="Review the book candidates"
              body="Surface the strongest recommendation first, then let the user keep moving without a heavy dashboard."
              accentClassName="bg-[linear-gradient(90deg,_var(--status-recommend),_rgba(143,166,140,0.35))]"
            />
          </div>
        </section>

        <section id="screenshots" className="grid gap-4 md:grid-cols-3">
          {highlights.map(({ icon: Icon, title, description }) => (
            <Card
              key={title}
              className="border-[var(--border-subtle)] bg-[var(--surface-elevated)] shadow-[0_12px_32px_rgba(58,48,42,0.05)]"
            >
              <CardHeader>
                <div className="flex h-10 w-10 items-center justify-center rounded-[var(--radius-md)] bg-[var(--accent-subtle)] text-[var(--accent)]">
                  <Icon className="h-5 w-5" />
                </div>
                <CardTitle className="mt-3 text-[1.1rem]">
                  {title}
                </CardTitle>
                <CardDescription className="text-[var(--text-secondary)]">{description}</CardDescription>
              </CardHeader>
            </Card>
          ))}
        </section>

        <section className="rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-5 py-6 shadow-[0_12px_32px_rgba(58,48,42,0.05)] sm:px-6">
          <p className="text-[11px] font-semibold uppercase tracking-[0.24em] text-[var(--text-tertiary)]">Why it exists</p>
          <p className="mt-3 max-w-3xl text-base leading-7 text-[var(--text-secondary)]">
            Librory is not a public catalog. It is a private tool for people who want to make fast decisions in a
            second-hand bookshop, then carry the useful results back home.
          </p>
        </section>
      </div>
    </main>
  )
}
