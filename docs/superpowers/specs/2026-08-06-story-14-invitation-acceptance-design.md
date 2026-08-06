# Story 14 Invitation Acceptance Design

## Goal

Let an invitee preview and accept a family invitation while preserving the simple account rule that every first login creates a personal family.

## Flow

1. The invite URL opens `/family-invitations/{token}`.
2. The page loads the public invitation preview and displays the family name, invitee email, and expiry.
3. An anonymous user chooses Google or Microsoft. The start URL carries the same-site invitation path as a validated `returnUrl`.
4. The existing external login flow creates or reuses the account and redirects back to the invitation page.
5. The authenticated user explicitly clicks **Accept invitation**. The API verifies the account email, token state, and target member state.
6. On success, the client selects the invited family and navigates to `/app/home`. The personal family remains available in the family switcher.

## Security

- `returnUrl` accepts only an absolute-path local URL; external URLs are rejected and fall back to `/app/home`.
- The invitation token remains in the invitation path and is never logged or copied into arbitrary redirect URLs.
- Public preview reveals only family name, normalized invite email, target-member presence, and expiry.
- Acceptance remains authenticated and email-bound; the client never supplies a member id.

## UI

Use the existing LoginPage/Card/Button visual language. The invitation page has loading, invalid/expired, anonymous sign-in, authenticated email mismatch, accepting, accepted, and error states. Long tokens are not rendered as body text beyond the browser URL.

## Testing

- API tests cover safe return URL handling and callback redirection.
- Web tests cover preview loading, anonymous provider links retaining the invitation path, authenticated acceptance, and error states.
