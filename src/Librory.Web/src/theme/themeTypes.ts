export type ThemeName = 'classic-scholar' | 'modern-scout' | 'cozy-archive' | 'botanical-press'

export type ThemeTokens = {
  pageBg: string
  surfaceElevated: string
  surfaceSunken: string
  borderSubtle: string
  borderStrong: string
  textPrimary: string
  textSecondary: string
  textTertiary: string
  accent: string
  accentOnAccent: string
  accentSubtle: string
  accentMuted: string
  statusRecommend: string
  statusWarn: string
  statusAlert: string
  statusNeutral: string
  shadow1: string
  shadow2: string
  shadow3: string
  fontBody: string
  fontDisplay: string
  fontMono: string
}

export type ThemeDefinition = {
  label: string
  tokens: ThemeTokens
}

export type ThemeContextValue = {
  themeName: ThemeName
  setThemeName: (themeName: ThemeName) => void
  definition: ThemeDefinition
}
