# Story 01 Backend Login Design

## Goal

Implement the backend slice for `story-01` so Librory can support real Google and Microsoft sign-in, persist external identities, bootstrap the first family/member on first login, and issue the cookie session used by the rest of the API.

This design follows the Koviva-style model already reflected in the repo:

- external identities are identified by provider plus provider subject
- the app owns the family/member record after sign-in
- the authenticated API session is cookie-based
- the current family context is derived from claims on that cookie

## Scope

### In scope

- Google sign-in
- Microsoft sign-in
- External identity persistence by provider plus subject
- First-login bootstrap of a singleton family and initial member
- Reuse of an existing member when the same provider identity signs in again
- Cookie sign-in for the API session
- Current family context claims required by downstream endpoints
- Keep the existing development auth endpoints for local debugging

### Out of scope

- Frontend redirect wiring for the real provider flow
- Invitation emails or admin transfer
- Shared family creation as a user-facing flow
- Multi-tenant enterprise identity
- Removing dev auth

## Current State

The repo already has the domain pieces needed for this flow:

- `ExternalIdentityProvider` with `Google` and `Microsoft`
- `ExternalIdentity` as a provider plus subject record
- `Member.ExternalIdentities` on the domain model
- `FirstLoginFamilyBootstrapper`
- `SharedFamilyCreator`
- `ExternalIdentityResolver`
- `CurrentFamilyContextMiddleware` and claim types
- cookie authentication in the API host

One important gap is persistence: `Member.ExternalIdentities` is currently ignored by EF Core, so story-01 must add storage for linked identities.

## Proposed Architecture

Keep the implementation provider-agnostic at the application boundary, but map to Google and Microsoft in the auth endpoints.

### Layers

- **API layer**
  - exposes auth start/callback endpoints for Google and Microsoft
  - receives the validated external identity result
  - signs the user into the cookie session
  - keeps `/dev/auth/*` available for local development

- **Application layer**
  - resolves an incoming external identity to an existing member
  - bootstraps a new singleton family when no member exists yet
  - encapsulates the first-login flow so the API stays thin

- **Domain layer**
  - remains the source of truth for `Family`, `Member`, and `ExternalIdentity`
  - continues to own bootstrap and member-linking rules

- **Infrastructure layer**
  - persists external identity links
  - queries members by provider plus subject

## Data Model

Persist linked external identities as a separate relational structure rather than keeping them only in memory.

Suggested shape:

- `members` stays the parent table
- `member_external_identities` stores:
  - `member_id`
  - `provider`
  - `provider_subject`
  - `email`
  - `display_name`
  - `linked_at`

Constraints:

- unique on `(provider, provider_subject)`
- foreign key to `members`
- provider and subject are the lookup key for sign-in

This keeps email optional and non-authoritative, which matches the current domain model and the story requirements.

## Login Flow

### Existing member sign-in

1. User starts Google or Microsoft sign-in.
2. Provider returns a validated identity payload.
3. The backend resolves `provider + provider subject` to an existing member.
4. The backend refreshes any non-authoritative profile fields if needed.
5. The backend issues the cookie session with family/member claims.
6. The user lands in `/app/home`.

### First login

1. User starts Google or Microsoft sign-in.
2. Provider returns a validated identity payload.
3. The backend does not find an existing linked member.
4. The backend bootstraps a singleton family and initial admin member.
5. The backend links the external identity to that member.
6. The backend persists the family, member, and identity link atomically.
7. The backend issues the cookie session with family/member claims.

### Current family context

The cookie session must continue to carry the claims already used by `CurrentFamilyContextResolver`:

- family id
- member id
- member role
- preferred language

That keeps the existing middleware and family-scoped endpoint pattern intact.

## Endpoint Shape

The exact route names can stay close to the current dev auth shape so the frontend does not need a different mental model.

Recommended backend endpoints:

- `GET /auth/google/start`
- `GET /auth/google/callback`
- `GET /auth/microsoft/start`
- `GET /auth/microsoft/callback`
- `POST /auth/logout`

Development endpoints stay in place:

- `POST /dev/auth/login`
- `POST /dev/auth/logout`
- `POST /dev/bootstrap`

## Error Handling

The auth flow should fail closed.

- Unknown or invalid provider identity returns a sign-in failure instead of guessing a member.
- Duplicate identity links should be blocked by the unique database constraint and handled as a retry-safe conflict.
- Invalid callback data should not create a family or member.
- If bootstrap fails partway through, the transaction should roll back so no partial identity remains.

## Testing Strategy

Add coverage at three levels:

### Domain tests

- external identity linking remains provider+subject based
- first-login bootstrap creates a family, member, and linked identity
- the same provider identity is not linked twice to the same member

### Application tests

- external identity resolution finds the correct member
- bootstrap flow creates a singleton family for a new sign-in
- preferred language is preserved in the initial member

### API/integration tests

- successful sign-in issues a cookie session
- subsequent protected requests resolve the current family context
- a first login creates persisted family/member data
- logout clears the cookie session

## Rollout Notes

- Keep dev auth while the real provider flow is being added.
- Do not switch the frontend to the real provider flow until the API callback path is in place.
- If a future implementation needs Azure-backed identity instead of direct provider integration, keep the provider+subject domain model and replace only the auth adapter layer.

## Acceptance Criteria

- Google and Microsoft sign-in can resolve to a persistent member record by provider plus subject.
- First login creates a singleton family and admin member when no linked identity exists.
- Cookie auth still drives the rest of the app and API.
- `CurrentFamilyContextMiddleware` continues to resolve family-scoped requests from the signed-in cookie.
- Dev auth remains available for local development.
