# Librory API Reference

This page documents the current developer-facing API slice that is available in `story-10`.

## Docs And Auth

- Scalar is enabled in development only.
- Protected endpoints use cookie authentication.
- Use `POST /dev/auth/login` or `POST /dev/bootstrap` in local development, then reuse the authenticated cookie in Scalar with persistent auth enabled.
- `POST /dev/auth/logout` clears the development auth cookie.

## Development Endpoints

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

### `POST /dev/bootstrap`

Bootstraps the default local development identity.

Behavior:

- Uses the built-in `Demo Family` and `Demo Admin` values.
- Is idempotent for repeated local calls.
- Signs the caller in with the default dev identity.

### `POST /dev/auth/logout`

Clears the current development auth cookie.

Behavior:

- Returns `204 No Content` on success.
- Makes subsequent protected requests unauthenticated until login runs again.

## Family Endpoints

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

## Book Work Endpoints

### `POST /api/book-works`

Creates a book work.

Behavior:

- Returns a work without editions when no edition details are supplied.
- Creates an edition only when at least one of `isbn`, `format`, or `publicationYear` is present.

### `GET /api/book-works/{bookWorkId}`

Returns a single work with its editions.

## Wishlist Endpoints

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

### `POST /api/family/current/wishlist`

Creates a wishlist item for the current family.

Behavior:

- Accepts a title plus optional author, work, and edition references.
- Returns validation errors for missing required fields.
- Returns `400` when the requested work/edition combination is invalid.
