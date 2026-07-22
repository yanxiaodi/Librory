import { BookOpen, ScanSearch, Languages, LibraryBig } from 'lucide-react'
import { Button } from './components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from './components/ui/card'

const highlights = [
  {
    icon: ScanSearch,
    title: 'Shelf scan',
    description: 'Capture a shelf and get book candidates fast.',
  },
  {
    icon: LibraryBig,
    title: 'Family library',
    description: 'Track ownership across the whole household.',
  },
  {
    icon: Languages,
    title: 'Bilingual UI',
    description: 'English and Chinese from day one.',
  },
]

export default function App() {
  return (
    <div className="min-h-screen bg-[radial-gradient(circle_at_top,_rgba(113,82,255,0.14),_transparent_38%),linear-gradient(180deg,#f7f6f1_0%,#fffdf8_48%,#f1efe7_100%)] text-slate-900">
      <main className="mx-auto flex min-h-screen max-w-6xl flex-col justify-center px-6 py-12">
        <section className="grid gap-8 lg:grid-cols-[1.2fr_0.8fr] lg:items-center">
          <div className="space-y-6">
            <div className="inline-flex items-center gap-2 rounded-full border border-slate-200 bg-white/80 px-4 py-2 text-sm font-medium shadow-sm backdrop-blur">
              <BookOpen className="h-4 w-4" />
              Librory
            </div>
            <div className="space-y-4">
              <h1 className="max-w-2xl text-5xl font-semibold tracking-tight text-slate-950 sm:text-6xl">
                The second-hand book scout for families.
              </h1>
              <p className="max-w-2xl text-lg leading-8 text-slate-600">
                Scan a shelf, spot duplicates, and decide what to buy in seconds. Built for English and Chinese households.
              </p>
            </div>
            <div className="flex flex-wrap gap-3">
              <Button size="lg">Start scanning</Button>
              <Button size="lg" variant="outline">View library</Button>
            </div>
          </div>
          <Card className="border-slate-200/80 bg-white/85 shadow-[0_24px_80px_rgba(15,23,42,0.12)] backdrop-blur">
            <CardHeader>
              <CardTitle>Today’s shelf</CardTitle>
              <CardDescription>Recommendation and duplicate detection appear together.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {highlights.map(({ icon: Icon, title, description }) => (
                <div key={title} className="flex items-start gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                  <div className="mt-0.5 rounded-xl bg-white p-2 shadow-sm">
                    <Icon className="h-5 w-5 text-slate-900" />
                  </div>
                  <div>
                    <div className="font-medium text-slate-900">{title}</div>
                    <div className="text-sm text-slate-600">{description}</div>
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>
        </section>
      </main>
    </div>
  )
}
