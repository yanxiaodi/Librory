# Story 07b Design: Wishlist API

## Goal

Expose wishlist paging and item creation as a family-scoped API so the frontend can list, create, and fetch wishlist items without duplicating domain rules.

## Scope

In scope:

- `GET /api/family/current/wishlist`
- `POST /api/family/current/wishlist`
- `GET /api/family/current/wishlist/{wishlistItemId}`
- Request and response contracts for wishlist items
- API integration tests and API reference updates

Out of scope:

- Front-end pages or routing
- Converting wishlist items into owned copies
- External metadata provider lookup
- Recommendation scoring or ranking

## Design

Treat wishlist data as a family-scoped resource.

### List flow

The `GET` endpoint returns a paged newest-first view of the current family's wishlist.

### Create flow

The `POST` endpoint accepts a title and optional references to a work or edition. It resolves the current family and member from auth, validates the request, and stores the wishlist item for the current family.

### Fetch flow

The item `GET` endpoint returns a single wishlist item for the current family by id so the frontend can deep-link to created items.

## Behavior

- Missing auth returns `401 Unauthorized`
- Missing or unknown family context returns `404 Not Found`
- Invalid paging or create data returns `400 Bad Request`
- Wishlist reads and writes stay scoped to the current family

## Testing

Add API integration coverage for:

- paging a family wishlist
- creating a wishlist item
- reading a wishlist item back
- rejecting invalid work or edition references
- rejecting cross-family access with `404 Not Found`

## Risks

- Wishlist links to work and edition data should stay canonical so later import and scan flows can reuse the same references.
- Paging defaults should stay documented because the frontend will likely depend on them.
