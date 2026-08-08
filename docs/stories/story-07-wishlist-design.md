# Story 07 Design: Wishlist

## Goal

Let users save books they want to buy later without marking them as owned.

## Scope

In scope:

- Wishlist domain behavior
- Wishlist API CRUD and paging
- Family-scoped wishlist ownership

Out of scope:

- Manual intake
- Recommendation scoring
- Duplicate detection changes

## Design

Keep wishlist items separate from owned copies.

### Domain behavior

The domain can create wishlist items separately from owned copies and can reference a work, edition, or fuzzy match result.

### API behavior

Expose wishlist CRUD and paging as a family-scoped API so the frontend can list, create, and fetch wishlist items without duplicating domain rules.

## Behavior

- Wishlist items are scoped to the current family and member context
- The API can return a paged newest-first wishlist
- Wishlist items can later be converted into owned copies by a separate slice

## Testing

Add coverage for:

- creating wishlist items
- reading wishlist items back
- paging newest-first results
- keeping items family-scoped