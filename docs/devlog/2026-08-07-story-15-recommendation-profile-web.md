# 2026-08-07 Story 15 Recommendation Profile Web

## Delivered

- Added a typed frontend client for member-scoped recommendation profile GET/PUT endpoints.
- Added a mobile-first `Reading preferences` card to Settings.
- Added age, favorite/excluded author, genre, and style, language, notes, visibility, and family-recommendation controls.
- Added administrator member switching while keeping regular members limited to their own editable profile.
- Treated a missing profile as an empty form and preserved explicit `null` and `[]` clear semantics in the PUT payload.
- Kept forbidden profile responses read-only and did not render private profile fields from a rejected request.
- Added the current member ID to the frontend family session projection so Settings can select the right profile.

## Boundary

This web slice manages saved recommendation preferences only. It does not select scan targets, infer per-scan language, score books, or call AI workflows; those remain Story 16 and Story 09 work.

## Validation

- Focused API and component tests pass.
- Full frontend test, lint, and build results are recorded with the PR validation.
- Browser review was attempted, but the local Vite process did not remain listening on port 5173 in this environment, so Playwright returned `ERR_CONNECTION_REFUSED`. Automated tests, lint, and build remain green.
