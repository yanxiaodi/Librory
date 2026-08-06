# Story 14 Design: Family Membership and Invitations

## Goal

Allow one login account to participate in multiple families while supporting personal libraries, shared family groups, placeholder members without accounts, and secure email invitation onboarding.

## Scope

In scope:

- Separate account identity from family membership.
- Give every first-time account a personal family, including users who arrived through an invitation link.
- Allow an account to join multiple families.
- Create members without external identities for children or other household profiles.
- Invite a new member by email or bind an existing placeholder member.
- Accept, resend, revoke, expire, and supersede invitations.
- List accessible families and select the active family context.
- Deactivate and reactivate members without deleting historical data.
- Enforce family-admin permissions for membership management.

Out of scope:

- Email provider implementation details beyond the existing notification seam.
- Family billing or platform-level tenancy administration.
- Automatic merging of books or profiles across families.
- Direct administrator assignment of an arbitrary external identity without an invitation.

## Domain Model

The current `Member -> Family` and `Member -> ExternalIdentity` shape is insufficient for multi-family accounts. Introduce an account-level identity and make membership family-scoped:

```text
UserAccount
  └── ExternalIdentity[]

Family
  └── FamilyMembership[]
        └── RecommendationProfile
```

The implementation may retain `Member` as the domain name for `FamilyMembership`, but it must no longer treat the member as the account itself.

Each membership has a stable member id, family id, display name within that family, role, preferred UI language, active/deactivated state, and an optional linked user account.

## Registration Rules

- Every first external registration creates a personal singleton family and an initial admin membership.
- Accepting an invitation adds a membership in the invited family or links the invited placeholder member; it never removes or merges the personal family.
- A user who already has a personal family keeps it when accepting another family invitation.
- The account can switch between its personal family and invited families; each family remains data-isolated.

## Invitation Model

Use a dedicated `FamilyInvitation` record inspired by Koviva's classroom invitation flow.

Required state includes:

- family id
- optional target member id for binding a placeholder member
- normalized invitee email
- one-way token hash
- status: `Pending`, `Accepted`, `Expired`, `Revoked`, or `Superseded`
- created by membership and timestamp
- expiry timestamp, defaulting to seven days
- accepted account and timestamp
- revoked by membership and timestamp
- optional superseding invitation id

There are two creation paths:

1. Invite a new adult by email. Acceptance creates a new member in the family.
2. Invite an existing placeholder member. Acceptance links the accepting account to that member.

Only one pending invitation for the same family and normalized email may exist. Resending creates a new token and supersedes the previous pending invitation.

## API Direction

Family membership management:

- `GET /api/families`
- `POST /api/families`
- `GET /api/family/current/members`
- `POST /api/family/current/members`
- `PATCH /api/family/current/members/{memberId}`
- `POST /api/family/current/members/{memberId}/deactivate`
- `POST /api/family/current/members/{memberId}/reactivate`

Active family context:

- `POST /api/families/{familyId}/select`
- `GET /api/family/current`

Invitations:

- `GET /api/family/current/invitations`
- `POST /api/family/current/invitations`
- `POST /api/family/current/invitations/{invitationId}/resend`
- `POST /api/family/current/invitations/{invitationId}/revoke`
- `POST /api/family/current/members/{memberId}/invitation`
- `GET /api/family-invitations/{token}`
- `POST /api/family-invitations/{token}/accept`

All family-scoped mutations require an active membership and administrator role. Invitation acceptance must validate the signed-in account email against the normalized invitation email and must never trust a member id supplied by the client.

The emailed-link flow keeps the same frontend URL and token route shape used by Koviva. The frontend and API must treat the token as sensitive: application and access logs must redact it, the invitation page must send a restrictive `Referrer-Policy`, and the token must be exchanged only over HTTPS outside local development.

## Acceptance Criteria

- A first registration creates one personal family and one admin membership, including when the user arrived through an invitation link.
- Accepting an invitation preserves that personal family and adds the invited family membership.
- An existing account can accept an invitation and join another family without duplicating its account identity.
- The same account can list and switch between all families where it has an active membership.
- Family-scoped data is isolated by active family membership.
- An administrator can create a placeholder member without an external identity.
- A placeholder member can later be bound to exactly one account through a valid invitation.
- Only administrators can create, resend, revoke, deactivate, or reactivate memberships and invitations.
- New invitations default to `Member`; administrator promotion is a separate operation.
- Invitation tokens are stored hashed, expire after seven days, and are single-use.
- Resending supersedes the previous pending invitation.
- Deactivation preserves books, profiles, and audit history and prevents new scan targeting.
- Every protected endpoint rejects a caller who is not a member of the selected family.

## Explicit Follow-Up

- Complex permission hierarchies remain out of scope.
- Family data migration and cross-family copy/import are later features.
