import * as React from 'react'
import { ThemeContext } from './themeContext'
import { defaultThemeName, isThemeName, themeRegistry, themeStorageKey } from './themeRegistry'
import type { ThemeName } from './themeTypes'

type ThemeRootProps = {
  children: React.ReactNode
}

function readInitialTheme(): ThemeName {
  if (typeof window === 'undefined') {
    return defaultThemeName
  }

  const savedTheme = window.localStorage.getItem(themeStorageKey)
  return savedTheme !== null && isThemeName(savedTheme) ? savedTheme : defaultThemeName
}

export function ThemeRoot({ children }: ThemeRootProps) {
  const [themeName, setThemeName] = React.useState<ThemeName>(() => readInitialTheme())

  React.useEffect(() => {
    window.localStorage.setItem(themeStorageKey, themeName)
  }, [themeName])

  const definition = themeRegistry[themeName]

  const themeStyle = {
    '--page-bg': definition.tokens.pageBg,
    '--surface-elevated': definition.tokens.surfaceElevated,
    '--surface-sunken': definition.tokens.surfaceSunken,
    '--border-subtle': definition.tokens.borderSubtle,
    '--border-strong': definition.tokens.borderStrong,
    '--text-primary': definition.tokens.textPrimary,
    '--text-secondary': definition.tokens.textSecondary,
    '--text-tertiary': definition.tokens.textTertiary,
    '--accent': definition.tokens.accent,
    '--accent-on-accent': definition.tokens.accentOnAccent,
    '--accent-subtle': definition.tokens.accentSubtle,
    '--accent-muted': definition.tokens.accentMuted,
    '--status-recommend': definition.tokens.statusRecommend,
    '--status-warn': definition.tokens.statusWarn,
    '--status-alert': definition.tokens.statusAlert,
    '--status-neutral': definition.tokens.statusNeutral,
    '--shadow-1': definition.tokens.shadow1,
    '--shadow-2': definition.tokens.shadow2,
    '--shadow-3': definition.tokens.shadow3,
    '--font-body': definition.tokens.fontBody,
    '--font-display': definition.tokens.fontDisplay,
    '--font-mono': definition.tokens.fontMono,
    fontFamily: definition.tokens.fontBody,
  } as React.CSSProperties

  const contextValue = React.useMemo(
    () => ({
      themeName,
      setThemeName,
      definition,
    }),
    [definition, themeName],
  )

  return (
    <ThemeContext.Provider value={contextValue}>
      <div
        data-theme={themeName}
        className="min-h-screen bg-[var(--page-bg)] text-[var(--text-primary)] antialiased"
        style={themeStyle}
      >
        {children}
      </div>
    </ThemeContext.Provider>
  )
}
