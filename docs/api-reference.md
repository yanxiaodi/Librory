# Librory API Reference

This page documents the current developer-facing API slice that is available in `story-10` and the login/auth slice added for `story-01`.

For front-end integration planning, see `[docs/frontend-integration-guide.md](/D:/dev/Librory/docs/frontend-integration-guide.md)`.

## Quick Map

- Authentication: Google and Microsoft login plus logout
- Development: login, logout, and bootstrap
- Family: current family summary
- Books: book work create and read
- Books: manual intake create and read
- Recommendations: current member profile read and update
- Wishlist: paged list, create, and fetch

## Docs And Auth

- Scalar is enabled in development only.
- Protected endpoints use cookie authentication.
- Use `GET /auth/google/start` or `GET /auth/microsoft/start` for the real login flow, then reuse the authenticated cookie in Scalar with persistent auth enabled.
- Use `POST /dev/auth/login` or `POST /dev/bootstrap` in local development when you want to bypass provider sign-in.
- `POST /auth/logout` clears the app auth cookie and the external auth cookie.
- `POST /dev/auth/logout` clears the development auth cookie.

## Authentication

### `GET /auth/google/start`

Starts the Google sign-in flow.

Behavior:

- Redirects to the configured Google auth challenge.
- Returns to `/auth/google/callback` after the provider finishes the sign-in round trip.

### `GET /auth/google/callback`

Completes the Google sign-in flow.

Behavior:

- Exchanges the provider response for an external identity.
- Creates or resolves the linked member.
- Bootstraps the first singleton family and member on first login.
- Issues the app cookie and redirects to `/app/home`.

### `GET /auth/microsoft/start`

Starts the Microsoft sign-in flow.

Behavior:

- Redirects to the configured Microsoft auth challenge.
- Returns to `/auth/microsoft/callback` after the provider finishes the sign-in round trip.

### `GET /auth/microsoft/callback`

Completes the Microsoft sign-in flow.

Behavior:

- Exchanges the provider response for an external identity.
- Creates or resolves the linked member.
- Bootstraps the first singleton family and member on first login.
- Issues the app cookie and redirects to `/app/home`.

### `POST /auth/logout`

Clears the current authenticated session.

Behavior:

- Signs out the app cookie.
- Signs out the external auth cookie.
- Returns `204 No Content` on success.

## Development

### `POST /dev/auth/login`

Logs in a developer against a family and member context.

Request body:

```json
{
  "familyName": "Demo Family",
  "memberDisplayName": "Test Admin",
  "preferredLanguage": 0
}
```

Behavior:

- Creates the family if it does not already exist.
- Reuses the existing member when the same family name and member display name are used again.
- Signs the caller in with the current family and member claims.
- `preferredLanguage` uses the current enum mapping: `0 = English`, `1 = Chinese`.

Returns:

- `200 OK` with the created or reused family/member context.
- `400 Bad Request` when `familyName` or `memberDisplayName` is missing.

### `POST /dev/bootstrap`

Bootstraps the default local development identity.

Behavior:

- Uses the built-in `Demo Family` and `Demo Admin` values.
- Is idempotent for repeated local calls.
- Signs the caller in with the default dev identity.

Returns:

- `200 OK` with the default family/member context.
- `400 Bad Request` only if the internal bootstrap payload is invalid, which should not happen in normal use.

### `POST /dev/auth/logout`

Clears the current development auth cookie.

Behavior:

- Returns `204 No Content` on success.
- Makes subsequent protected requests unauthenticated until login runs again.

Returns:

- `204 No Content` on success.

## Family

### `GET /api/family/current`

Returns the current family summary for the signed-in member.

Response shape:

```json
{
  "familyId": "guid",
  "familyName": "Demo Family",
  "memberId": "guid",
  "memberDisplayName": "Test Admin",
  "memberRole": 1,
  "preferredLanguage": 0,
  "memberCount": 1,
  "bookCount": 0,
  "wishlistCount": 0
}
```

Notes:

- `/api/me` is no longer mapped.
- This route is the canonical current-family endpoint.
- `memberRole` uses the current enum mapping: `0 = Member`, `1 = Admin`.

Returns:

- `200 OK` with the family summary when authenticated.
- `401 Unauthorized` when no valid family context is present.
- `404 Not Found` when the cookie points at a family that no longer exists.

## Books

### `POST /api/book-works`

Creates a book work.

Behavior:

- Returns a work without editions when no edition details are supplied.
- Creates an edition only when at least one of `isbn`, `format`, or `publicationYear` is present.

Returns:

- `201 Created` with the persisted work and its editions.
- `400 Bad Request` when the title is blank.

### `GET /api/book-works/{bookWorkId}`

Returns a single work with its editions.

Returns:

- `200 OK` with the work payload.
- `404 Not Found` when the work id does not exist.

### `POST /api/family/current/book-copies`

Creates a book copy for the current family using a resolved edition.

Behavior:

- Attaches the copy to the current signed-in member.
- Accepts optional purchase metadata and intake notes.
- Returns the duplicate warning summary alongside the created copy.

Returns:

- `201 Created` with the created copy payload and duplicate summary.
- `400 Bad Request` when the intake data is invalid.
- `401 Unauthorized` when the caller is not signed in.
- `404 Not Found` when the referenced edition does not exist.

### `GET /api/family/current/book-copies/{bookCopyId}`

Returns a single book copy for the current family.

Returns:

- `200 OK` with the copy payload.
- `401 Unauthorized` when the caller is not signed in.
- `404 Not Found` when the copy does not exist for the current family.

## Metadata

### `GET /api/book-metadata/search`

Searches external book metadata by title.

Query parameters:

- `title` is required
- `language` is optional and passes through to the metadata provider
- `maxResults` defaults to `10` and must stay between `1` and `40`

Behavior:

- Searches the configured metadata provider for matching books.
- Returns normalized book metadata instead of raw provider JSON.
- Uses the provider result order as the default relevance order.

Returns:

- `200 OK` with normalized search results.
- `400 Bad Request` when `title` is missing or `maxResults` is invalid.

## Recommendations

### `GET /api/family/current/recommendation-profile`

Returns the current member's recommendation profile when one exists.

Returns:

- `200 OK` with the profile payload.
- `401 Unauthorized` when the caller is not signed in.
- `404 Not Found` when the current member has not created a recommendation profile yet.

### `PUT /api/family/current/recommendation-profile`

Creates or updates the current member's recommendation profile.

Behavior:

- Creates the profile when it does not already exist.
- Preserves existing values when fields are omitted.
- `null` request fields preserve existing values; there is no explicit clear operation in this slice.
- Lets the domain continue enforcing age-range validation and preference normalization.

Returns:

- `200 OK` with the saved profile payload.
- `400 Bad Request` when the profile data is invalid.
- `401 Unauthorized` when the caller is not signed in.

## Scan Sessions

### `POST /api/family/current/scan-sessions`

Creates a temporary scan session for the current family.

Behavior:

- Requires a shelf photo path.
- Accepts optional recognized candidates.
- Accepts an optional retention window in days.
- Stores the session temporarily for later review.

Returns:

- `201 Created` with the persisted session payload.
- `400 Bad Request` when required fields are missing or invalid.
- `401 Unauthorized` when the caller is not signed in.
- `404 Not Found` when the current family no longer exists.

### `GET /api/family/current/scan-sessions/{scanSessionId}`

Returns a temporary scan session for the current family.

Behavior:

- Returns the stored scan session and its candidates.
- Treats expired sessions as not found.

Returns:

- `200 OK` with the persisted session payload.
- `401 Unauthorized` when the caller is not signed in.
- `404 Not Found` when the session does not exist, belongs to another family, or has expired.

### `PUT /api/family/current/scan-sessions/{scanSessionId}/candidates/{candidateId}`

Corrects a single scan candidate in place.

Behavior:

- Updates the matching candidate without resetting the rest of the session.
- Reuses the same correction fields as the candidate creation shape.
- Returns the full updated session after the correction is saved.

Returns:

- `200 OK` with the updated session payload.
- `400 Bad Request` when the correction data is invalid.
- `401 Unauthorized` when the caller is not signed in.
- `404 Not Found` when the session or candidate does not exist for the current family.

### `POST /api/family/current/scan-sessions/{scanSessionId}/candidates/{candidateId}/resolve`

Promotes a scan candidate into canonical book catalog data.

Behavior:

- Creates a canonical `BookWork` from the candidate data.
- Creates the first edition when edition details are supplied.
- Removes the candidate from the temporary scan session after successful promotion.

Returns:

- `201 Created` with the canonical book work payload.
- `400 Bad Request` when the resolution data is invalid.
- `401 Unauthorized` when the caller is not signed in.
- `404 Not Found` when the session or candidate does not exist for the current family.

### `DELETE /api/family/current/scan-sessions/{scanSessionId}/candidates/{candidateId}`

Discards a scan candidate from the temporary session without promoting it.

Behavior:

- Removes the candidate from the scan session.
- Leaves the canonical catalog untouched.

Returns:

- `204 No Content` on success.
- `401 Unauthorized` when the caller is not signed in.
- `404 Not Found` when the session or candidate does not exist for the current family.

## Wishlist

### `GET /api/family/current/wishlist`

Returns a paged wishlist for the current family.

Query parameters:

- `page` defaults to `1`
- `pageSize` defaults to `20`
- `pageSize` must stay between `1` and `100`

Response shape:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0
}
```

Behavior:

- Items are ordered by newest first.
- The response includes the current page, the page size, and the total matching item count.
- Use `page` and `pageSize` to page through large family wishlists without loading the full result set.

Returns:

- `200 OK` with the current page of items.
- `400 Bad Request` when `page` is less than `1` or `pageSize` is outside `1..100`.
- `401 Unauthorized` when the caller is not signed in.

### `POST /api/family/current/wishlist`

Creates a wishlist item for the current family.

Behavior:

- Accepts a title plus optional author, work, and edition references.
- Returns validation errors for missing required fields.
- Returns `400` when the requested work/edition combination is invalid.

Returns:

- `201 Created` with the persisted wishlist item.
- `400 Bad Request` when the title is missing or the requested work/edition combination is invalid.
- `401 Unauthorized` when the caller is not signed in.
- `404 Not Found` when the referenced work or edition does not exist.

### `GET /api/family/current/wishlist/{wishlistItemId}`

Returns a single wishlist item for the current family.

Returns:

- `200 OK` with the wishlist item payload.
- `401 Unauthorized` when the caller is not signed in.
- `404 Not Found` when the item does not exist for the current family.

## Pending API Slices

These backend story slices still need dedicated API work before the frontend can rely on them directly:

- `story-12` External metadata providers and canonical import

Planned capabilities for that slice:

- lookup by ISBN
- canonical import or promotion of confirmed external metadata
- provider selection when multiple metadata sources are enabled
