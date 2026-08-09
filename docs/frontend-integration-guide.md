# Frontend Integration Guide

This guide summarizes the API surface that the frontend can integrate against now, grouped by user flow instead of by backend story.

## What Is Ready

Ready for frontend integration:

- Google sign-in
- Microsoft sign-in
- Logout
- Development login / logout / bootstrap
- Current family summary
- Book work create and read
- Manual intake create and read
- Book metadata title search
- Recommendation profile read and update
- Scan session create, read, correct, resolve, and discard
- Wishlist list, create, and fetch
- Book recognition job create and poll
- Book recognition job result rendering in the scan flow

Still pending:

None.

## Global Rules

- The user-facing login flow should use `/auth/google/start` or `/auth/microsoft/start`.
- Auth uses the development cookie in local environments.
- In Development, the API applies EF Core migrations on startup, so local Aspire runs should not require a separate manual migration step.
- All family-scoped endpoints require the current family context to be present.
- `401 Unauthorized` means there is no usable auth context.
- `404 Not Found` on family-scoped resources usually means the item does not belong to the current family.
- Recommendation profile updates preserve existing values when request fields are omitted or set to `null`.
- Wishlist list endpoints are paged and newest-first.

## Recommended Frontend Sequence

1. Visit `/` for the public landing page.
2. Sign in at `/login`.
3. Click Google or Microsoft.
4. Land on `/app/home`.
5. Load `GET /api/family/current` to prime the shell.
6. Route to the relevant work area:
   - manual intake
   - recommendation profile
   - wishlist
   - scan sessions
7. Use sign out from settings to return to `/login`.

## Current Family Shell

Use this first after login to get the active family/member context and the scan-first home summary.

- `GET /api/family/current`

If you need to test the backend auth slice directly, start login with:

- `GET /auth/google/start`
- `GET /auth/microsoft/start`

For local debug bypasses, you can still use:

- `POST /dev/auth/login`
- `POST /dev/bootstrap`

Use the response to populate:

- family name
- member display name
- role
- preferred language
- counters for books and wishlist items

## Manual Intake Flow

Use this when the user already knows the edition they want to record.

Recommended sequence:

1. Resolve or choose a `bookEditionId`.
2. Call `POST /api/family/current/book-copies`.
3. Use the returned created copy payload to navigate to the copy detail view if needed.
4. Call `GET /api/family/current/book-copies/{bookCopyId}` when you need to re-fetch the created record.

Endpoints:

- `POST /api/family/current/book-copies`
- `GET /api/family/current/book-copies/{bookCopyId}`

Notes:

- The slice does not do ISBN lookup.
- Duplicate detection is warning-only, not a hard block.
- Optional intake metadata can be added at create time.

## Book Metadata Search Flow

Use this when you already have a candidate title from Document Intelligence or user input.

Recommended sequence:

1. Call `GET /api/book-metadata/search?title=...`.
2. Filter or rank the returned candidates in the UI.
3. Send the chosen metadata to the next scan or recommendation step.

Endpoints:

- `GET /api/book-metadata/search`

Notes:

- The API returns normalized provider data rather than raw Google Books JSON.
- `language` and `maxResults` are optional query parameters.
- This is the first slice of the external metadata provider work; ISBN lookup and canonical import are still pending.

## Recommendation Profile Flow

Use this for per-member reading preferences.

Recommended sequence:

1. Call `GET /api/family/current/recommendation-profile`.
2. If not found, show an empty form.
3. On save, call `PUT /api/family/current/recommendation-profile`.
4. Reuse the returned profile payload as the canonical form state.

Endpoints:

- `GET /api/family/current/recommendation-profile`
- `PUT /api/family/current/recommendation-profile`

Notes:

- Partial updates preserve existing values.
- Empty fields are not treated as explicit clears in this API slice.
- Invalid age ranges are rejected by the domain.

## Scan Session Flow

Use this for shelf-photo review and candidate correction.

Recommended sequence:

1. Create a temporary session with `POST /api/family/current/scan-sessions`.
2. Load or refresh the session with `GET /api/family/current/scan-sessions/{scanSessionId}`.
3. Correct a single candidate with `PUT /api/family/current/scan-sessions/{scanSessionId}/candidates/{candidateId}`.
4. Promote a candidate into canonical catalog data with `POST /api/family/current/scan-sessions/{scanSessionId}/candidates/{candidateId}/resolve`.
5. Discard a candidate with `DELETE /api/family/current/scan-sessions/{scanSessionId}/candidates/{candidateId}`.

Endpoints:

- `POST /api/family/current/scan-sessions`
- `GET /api/family/current/scan-sessions/{scanSessionId}`
- `PUT /api/family/current/scan-sessions/{scanSessionId}/candidates/{candidateId}`
- `POST /api/family/current/scan-sessions/{scanSessionId}/candidates/{candidateId}/resolve`
- `DELETE /api/family/current/scan-sessions/{scanSessionId}/candidates/{candidateId}`

Notes:

- Treat scan sessions as temporary UI state backed by the backend.
- The session shape is likely to change sooner than the family or book-copy resources.
- Downstream duplicate/recommendation refresh is not owned by this API slice.

## Wishlist Flow

Use this for future-to-buy books.

Recommended sequence:

1. Load the wishlist with `GET /api/family/current/wishlist`.
2. Page through results using `page` and `pageSize`.
3. Create a wishlist item with `POST /api/family/current/wishlist`.
4. Fetch an item by id with `GET /api/family/current/wishlist/{wishlistItemId}`.

Endpoints:

- `GET /api/family/current/wishlist`
- `POST /api/family/current/wishlist`
- `GET /api/family/current/wishlist/{wishlistItemId}`

Notes:

- Default pagination is `page=1` and `pageSize=20`.
- Valid `pageSize` values are `1` through `100`.
- Items are returned newest-first.

## What Is Still Missing

No backend story slices are currently missing frontend-facing API support, and the scan flow recognition results are now rendered in the web app.

## Suggested Frontend Order

If you want to start UI integration immediately, the best order is:

1. Current family shell
2. Wishlist list/create/detail
3. Recommendation profile
4. Manual intake
5. Scan sessions

That order gives you a usable shell and a low-risk data loop before you touch the more temporary scan/session flows.
