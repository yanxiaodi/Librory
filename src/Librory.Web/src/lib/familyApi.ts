export type FamilySummary = {
  familyId: string
  familyName: string
  memberId: string
  memberDisplayName: string
  role: string
  isActive: boolean
}

export type FamilyMember = {
  memberId: string
  displayName: string
  role: string
  preferredLanguage: number | string
  isActive: boolean
  hasAccount: boolean
  hasRecommendationProfile?: boolean
  recommendationProfileVisibility?: number | string | null
  canUseForFamilyRecommendations?: boolean
}

export type RecommendationProfile = {
  memberId: string
  minimumAge: number | null
  maximumAge: number | null
  favoriteAuthors: string[]
  excludedAuthors: string[]
  favoriteGenres: string[]
  excludedGenres: string[]
  favoriteStyles: string[]
  excludedStyles: string[]
  preferredBookLanguages: Array<number | string>
  preferenceNotes: string | null
  profileVisibility: number | string
  useInFamilyRecommendations: boolean
}

export type RecommendationProfileUpdate = Omit<RecommendationProfile, 'memberId'>

export type FamilyInvitation = {
  invitationId: string
  familyId: string
  targetMemberId: string | null
  email: string
  status: string
  createdAt: string
  expiresAt: string
  invitationUrl?: string | null
}

export type InvitationPreview = {
  id: string
  familyName: string
  email: string
  targetMemberId: string | null
  expiresAt: string
}

export type CreateMemberInput = {
  displayName: string
  preferredLanguage?: number
}

export type UpdateMemberInput = {
  displayName?: string
  preferredLanguage?: number
  role?: string
}

export type CreateInvitationInput = {
  email: string
  targetMemberId?: string
}

export class FamilyApiError extends Error {
  constructor(public readonly status: number, message: string) {
    super(`Family API request failed (${status}): ${message}`)
    this.name = 'FamilyApiError'
  }
}

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    ...init,
    credentials: 'include',
  })

  if (!response.ok) {
    let message = response.statusText || 'Request failed.'
    try {
      const body = (await response.json()) as { title?: string; message?: string }
      message = body.title ?? body.message ?? message
    } catch {
      // Keep the HTTP status text when the response has no JSON body.
    }
    throw new FamilyApiError(response.status, message)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}

function jsonRequest(method: string, body: unknown): RequestInit {
  return {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  }
}

export const listFamilies = () => request<FamilySummary[]>('/api/families')

export const selectFamily = (familyId: string) =>
  request<FamilySummary>(`/api/families/${familyId}/select`, { method: 'POST' })

export const listMembers = () => request<FamilyMember[]>('/api/family/current/members')

export const createMember = (input: CreateMemberInput) =>
  request<FamilyMember>('/api/family/current/members', jsonRequest('POST', input))

export const updateMember = (memberId: string, input: UpdateMemberInput) =>
  request<FamilyMember>(`/api/family/current/members/${memberId}`, jsonRequest('PATCH', input))

export const setMemberActive = (memberId: string, active: boolean) =>
  request<FamilyMember>(`/api/family/current/members/${memberId}/${active ? 'reactivate' : 'deactivate'}`, { method: 'POST' })

export const getMemberRecommendationProfile = (memberId: string) =>
  request<RecommendationProfile>(`/api/family/current/members/${memberId}/recommendation-profile`)

export const updateMemberRecommendationProfile = (memberId: string, input: RecommendationProfileUpdate) =>
  request<RecommendationProfile>(
    `/api/family/current/members/${memberId}/recommendation-profile`,
    jsonRequest('PUT', input),
  )

export const listInvitations = () => request<FamilyInvitation[]>('/api/family/current/invitations')

export const createInvitation = (input: CreateInvitationInput) =>
  request<FamilyInvitation>('/api/family/current/invitations', jsonRequest('POST', input))

export const resendInvitation = (invitationId: string) =>
  request<FamilyInvitation>(`/api/family/current/invitations/${invitationId}/resend`, { method: 'POST' })

export const revokeInvitation = (invitationId: string) =>
  request<FamilyInvitation>(`/api/family/current/invitations/${invitationId}/revoke`, { method: 'POST' })

export const getInvitationPreview = (token: string) =>
  request<InvitationPreview>(`/api/family-invitations/${encodeURIComponent(token)}`)

export const acceptInvitation = (token: string) =>
  request<FamilySummary>(`/api/family-invitations/${encodeURIComponent(token)}/accept`, { method: 'POST' })
