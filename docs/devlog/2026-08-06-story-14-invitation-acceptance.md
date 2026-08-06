# Story 14 invitation acceptance

## What changed

- Preserved a validated same-site `returnUrl` through Google/Microsoft login callbacks.
- Added typed invitation preview and acceptance API calls.
- Added `/family-invitations/:token` with preview, provider sign-in links, explicit acceptance, and error states.
- Acceptance selects the invited family after the account is linked; the account's personal family remains available.

## Validation

- Backend: 177 tests passed across Domain, Application, and API projects.
- Web: lint passed, 11 test files / 24 tests passed, production build passed.
- Browser review remains subject to the local Vite server connection limitation recorded in the previous Web devlog.
