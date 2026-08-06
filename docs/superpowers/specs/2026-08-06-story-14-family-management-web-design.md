# Story 14 Family Management Web Design

## Goal

Provide a mobile-first family management surface in the existing Settings page so an authenticated user can switch families, manage family members, and manage invitations.

## Scope

This slice includes:

- listing active families and selecting the current family;
- listing family members;
- creating placeholder members;
- updating member display name, language, and role;
- deactivating and reactivating members;
- listing, creating, resending, and revoking family invitations.

Invitation-link preview, invitation acceptance, and unauthenticated invitation registration are intentionally deferred to a later slice.

## Design

`SettingsPage` remains the entry point. The page keeps the existing `PageFrame` wrapper and uses three stacked sections inside the current single-column mobile layout:

1. **Family** — a compact current-family summary and a select control populated by `GET /api/families`; changing it calls `POST /api/families/{familyId}/select`, then refreshes the auth session.
2. **Members** — a list of members with role, active state, and account-link state. Admins can add a placeholder member, edit member details, and deactivate/reactivate members.
3. **Invitations** — a list of invitation statuses. Admins can create an email invitation, resend a pending invitation, or revoke it. The returned one-time invitation URL is shown only after create/resend with a copy action; list responses never expose it.

All cards use existing `surface-elevated`, `border-subtle`, `text-*`, `accent-*`, radius, and shadow tokens. Existing `Button`, `PageFrame`, and form-control patterns are reused. No new colors, global layout, navigation item, or state library is introduced.

## Client boundary

Create `src/Librory.Web/src/lib/familyApi.ts` as the only API boundary for this slice. It exposes typed functions for families, members, and invitations, uses `credentials: 'include'`, parses JSON responses, and throws an error containing the server message/status for failed requests. Components do not construct endpoint URLs directly.

The API client treats `InvitationUrl` as an optional response field. It is held in component state only for the just-created/resend result and is cleared when the invitation panel is refreshed.

## Error and loading behavior

- Initial loading displays a muted inline loading state in each section.
- Mutations disable their triggering control while pending.
- Failed requests show an inline alert in the section that initiated the request.
- A successful mutation refreshes only the affected section.
- If the current family selection succeeds, `refreshSession()` updates the global family summary used by the shell.

## Testing

- API client tests mock `fetch` and verify URL, method, credentials, JSON body, and error handling.
- Settings tests cover family loading and selection, member creation, invitation creation with one-time URL display, and admin actions.
- Existing theme and logout tests remain unchanged and must continue to pass.

## Deferred work

- `/family-invitations/{token}` frontend preview page;
- unauthenticated invitation registration and account linking;
- notification delivery integration;
- localized UI copy beyond the current English interface.
