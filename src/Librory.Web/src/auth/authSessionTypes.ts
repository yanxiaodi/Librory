export type AuthStatus = 'loading' | 'anonymous' | 'authenticated'

export type FamilySummary = {
  id: string
  name: string
  memberCount: number
}

export type AuthUser = {
  id: string
  displayName: string
  email?: string | null
  role?: string
}

export type AuthSession = {
  status: AuthStatus
  user: AuthUser | null
  family: FamilySummary | null
}
