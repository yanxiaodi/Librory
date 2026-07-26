import { PageFrame } from '@/components/shell/PageFrame'
import { ThemeSelect } from '@/components/theme/ThemeSelect'

export default function SettingsPage() {
  return (
    <PageFrame
      eyebrow="Settings"
      title="Preferences"
      description="Keep appearance controls here for now. Style selection is live, while language and account settings remain future placeholders."
    >
      <div className="grid gap-4">
        <ThemeSelect />
        <div className="rounded-[14px] border border-dashed border-[var(--border-subtle)] bg-[var(--accent-muted)] px-4 py-4 font-[family-name:var(--font-body)] text-sm text-[var(--text-secondary)]">
          Language preferences and family-level settings will be added here later.
        </div>
      </div>
    </PageFrame>
  )
}
