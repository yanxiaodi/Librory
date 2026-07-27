import * as React from 'react'
import { Check, ChevronDown } from 'lucide-react'
import { useTheme } from '@/theme/useTheme'
import { themeOrder, themeRegistry } from '@/theme/themeRegistry'
import type { ThemeName } from '@/theme/themeTypes'

export function ThemeSelect() {
  const { themeName, setThemeName } = useTheme()
  const [isOpen, setIsOpen] = React.useState(false)
  const rootRef = React.useRef<HTMLDivElement | null>(null)
  const listId = React.useId()

  React.useEffect(() => {
    function handlePointerDown(event: PointerEvent) {
      if (rootRef.current?.contains(event.target as Node)) {
        return
      }

      setIsOpen(false)
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setIsOpen(false)
      }
    }

    document.addEventListener('pointerdown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)

    return () => {
      document.removeEventListener('pointerdown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [])

  function handleSelect(nextTheme: ThemeName) {
    setThemeName(nextTheme)
    setIsOpen(false)
  }

  return (
    <div ref={rootRef} className="flex flex-col gap-2">
      <span className="font-[family-name:var(--font-body)] text-sm font-semibold tracking-[0.02em] text-[var(--text-secondary)]">
        Style
      </span>
      <button
        type="button"
        aria-haspopup="listbox"
        aria-expanded={isOpen}
        aria-controls={listId}
        className="flex h-12 w-full items-center justify-between rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] px-4 text-left font-[family-name:var(--font-body)] text-sm text-[var(--text-primary)] transition hover:border-[var(--border-strong)] hover:bg-[var(--surface-sunken)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-subtle)]"
        onClick={() => setIsOpen((value) => !value)}
      >
        <span className="font-medium">{themeRegistry[themeName].label}</span>
        <ChevronDown className={`h-4 w-4 text-[var(--text-tertiary)] transition ${isOpen ? 'rotate-180' : ''}`} />
      </button>

      {isOpen ? (
        <div className="rounded-[var(--radius-md)] border border-[var(--border-subtle)] bg-[var(--surface-elevated)] p-1 shadow-[0_12px_24px_rgba(58,48,42,0.08)]">
          <div
            id={listId}
            role="listbox"
            aria-label="Style options"
            className="grid gap-1"
          >
            {themeOrder.map((themeKey) => {
              const active = themeKey === themeName

              return (
                <button
                  key={themeKey}
                  type="button"
                  role="option"
                  aria-selected={active}
                  className={[
                    'flex items-center justify-between rounded-[var(--radius-md)] px-3 py-2.5 text-left font-[family-name:var(--font-body)] text-sm transition',
                    active
                      ? 'bg-[var(--accent-subtle)] text-[var(--text-primary)]'
                      : 'text-[var(--text-secondary)] hover:bg-[var(--surface-sunken)] hover:text-[var(--text-primary)]',
                  ].join(' ')}
                  onClick={() => handleSelect(themeKey)}
                >
                  <span className="flex flex-col">
                    <span className="font-medium">{themeRegistry[themeKey].label}</span>
                    {themeKey === 'botanical-press' ? (
                      <span className="text-[11px] uppercase tracking-[0.18em] text-[var(--text-tertiary)]">Default</span>
                    ) : null}
                  </span>
                  {active ? <Check className="h-4 w-4 text-[var(--accent)]" /> : null}
                </button>
              )
            })}
          </div>
        </div>
      ) : null}
    </div>
  )
}
