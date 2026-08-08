# Story 10 Design: API and Persistence Foundation

## Goal

Stand up the first production-shaped backend slice with PostgreSQL, EF Core, a design-time DbContext, and developer-friendly API docs and auth so the existing domain and application work can run against a real backend.

## Scope

In scope:

- PostgreSQL persistence
- EF Core migrations and design-time factory
- Scalar API docs
- Local development auth tooling
- Core family, book, and wishlist persistence support

Out of scope:

- Product-grade login UX
- Hand-authored migration SQL
- New business behavior beyond the foundation slice

## Design

Keep the foundation slice production-shaped and tooling-friendly.

### Persistence

Use PostgreSQL through EF Core for application data.

### Tooling

Provide a design-time DbContext factory and enable Scalar documentation.

### Local auth

Support developer authentication in local development so secured endpoints can be exercised during debugging.

## Behavior

- The API can start against a local PostgreSQL database
- The current family endpoint is canonical at `/api/family/current`
- The wishlist endpoint supports paging for larger families

## Testing

Add coverage for:

- starting against PostgreSQL
- applying migrations through tooling
- calling secured endpoints with local auth