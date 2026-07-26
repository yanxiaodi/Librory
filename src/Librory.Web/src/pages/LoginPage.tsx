import { Link } from 'react-router-dom'
import { BookOpen, Landmark } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader } from '@/components/ui/card'
import { authEndpoints } from '@/auth/authEndpoints'

const providers = [
  {
    label: 'Google',
    icon: BookOpen,
    href: authEndpoints.googleStart,
  },
  {
    label: 'Microsoft',
    icon: Landmark,
    href: authEndpoints.microsoftStart,
  },
] as const

export default function LoginPage() {
  return (
    <main className="min-h-screen bg-[linear-gradient(180deg,_var(--page-bg)_0%,_#f7f2ee_100%)] px-5 py-6 text-[var(--text-primary)]">
      <div className="mx-auto flex min-h-[calc(100vh-3rem)] max-w-md items-center">
        <Card className="w-full border-[var(--border-subtle)] bg-[var(--surface-elevated)] shadow-[0_18px_48px_rgba(58,48,42,0.08)]">
          <CardHeader>
            <p className="text-[11px] font-semibold uppercase tracking-[0.24em] text-[var(--text-tertiary)]">Librory</p>
            <h1 className="mt-2 font-[family-name:var(--font-display)] text-[2rem] font-normal italic text-[var(--text-primary)]">
              Sign in
            </h1>
            <CardDescription className="text-[var(--text-secondary)]">
              Use Google, Microsoft, or email to enter your family space. If you have not created one yet, the app
              will treat you as a family of one.
            </CardDescription>
          </CardHeader>

          <CardContent className="grid gap-3">
            {providers.map((provider) => {
              const { label, icon: Icon, href } = provider

              return (
                <Button
                  key={label}
                  asChild
                  variant="outline"
                  size="lg"
                  className="justify-start border-[var(--border-subtle)] bg-white px-4 text-[var(--text-primary)] hover:bg-[var(--surface-sunken)]"
                >
                  <a href={href}>
                    <Icon className="h-4 w-4" />
                    Continue with {label}
                  </a>
                </Button>
              )
            })}

            <p className="pt-2 text-sm leading-6 text-[var(--text-secondary)]">
              By continuing, you agree to use Librory as a private app for your own books or family library.
            </p>

            <Button asChild variant="outline" className="mt-1 border-[var(--border-subtle)] bg-transparent text-[var(--text-primary)]">
              <Link to="/">Back to landing</Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    </main>
  )
}
