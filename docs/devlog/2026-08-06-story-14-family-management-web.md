# Story 14 family management web slice

## What changed

- Added a typed `familyApi` boundary for family, member, and invitation operations.
- Replaced the Settings placeholder with family selection, member management, and invitation management cards.
- Added admin-only controls for placeholder members and invitation lifecycle actions.
- Kept one-time invitation URLs in create/resend state only and provided a copy action.
- Reused the existing PageFrame, Card, Button, theme tokens, and mobile-first layout.

## Validation

- `npm run lint` passed.
- `npm run test:run` passed: 10 files, 21 tests.
- `npm run build` passed.
- Playwright browser review was attempted, but the local Vite server could not be reached at `127.0.0.1:5173` in this environment.

Invitation acceptance and unauthenticated invitation registration remain deferred to the next slice.
