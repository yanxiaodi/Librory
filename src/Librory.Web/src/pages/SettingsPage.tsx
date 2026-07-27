import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { PageFrame } from '@/components/shell/PageFrame'
import { useAuthSessionActions } from '@/auth/AuthSessionContext'
import { ThemeSelect } from '@/components/theme/ThemeSelect'

export default function SettingsPage() {
  const navigate = useNavigate()
  const { signOut } = useAuthSessionActions()

  const handleSignOut = async () => {
    await signOut()
    navigate('/login', { replace: true })
  }

  return (
    <PageFrame
      eyebrow="Settings"
      title="Preferences"
      description="Keep appearance controls here for now. Style selection is live, while language and account settings remain future placeholders."
    >
      <div className="grid gap-4">
        <ThemeSelect />
        <div className="rounded-[var(--radius-md)] border border-dashed border-[var(--border-subtle)] bg-[var(--accent-muted)] px-4 py-4 font-[family-name:var(--font-body)] text-sm text-[var(--text-secondary)]">
          Language preferences and family-level settings will be added here later.
        </div>
        <Button
          type="button"
          variant="outline"
          className="justify-start text-[var(--text-primary)]"
          onClick={() => void handleSignOut()}
        >
          Sign out
        </Button>
      </div>
    </PageFrame>
  )
}
