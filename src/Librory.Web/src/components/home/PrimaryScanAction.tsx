import { ScanSearch } from 'lucide-react'
import { Button } from '@/components/ui/button'

export function PrimaryScanAction() {
  return (
    <Button size="lg" className="h-14 w-full justify-start bg-[var(--accent)] text-[var(--accent-on-accent)] hover:bg-[var(--accent)]/90">
      <ScanSearch className="h-5 w-5" />
      Scan a shelf
    </Button>
  )
}
