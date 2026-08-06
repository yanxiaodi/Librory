import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { PageFrame } from '@/components/shell/PageFrame'
import { useAuthSession, useAuthSessionActions } from '@/auth/AuthSessionContext'
import { ThemeSelect } from '@/components/theme/ThemeSelect'
import { FamilySection } from '@/components/family/FamilySection'
import { MembersSection } from '@/components/family/MembersSection'

export default function SettingsPage() {
  const navigate = useNavigate()
  const session = useAuthSession()
  const { signOut, refreshSession } = useAuthSessionActions()
  const [familyRefreshKey, setFamilyRefreshKey] = useState(0)

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
        {session.status === 'authenticated' ? (
          <>
            <FamilySection
              currentFamilyId={session.family?.id}
              onFamilySelected={async () => {
                await refreshSession()
                setFamilyRefreshKey(value => value + 1)
              }}
            />
            <MembersSection isAdmin={session.user?.role === 'Admin'} refreshKey={familyRefreshKey} />
          </>
        ) : null}
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
