# Story 12 Design: External Metadata Providers and Canonical Import

## Goal

Normalize book metadata from external providers into Librory's canonical catalog records so later scan, wishlist, and intake flows can reuse the same source of truth.

## Scope

In scope:

- External provider lookup by ISBN and title
- Canonical import of confirmed results
- Provider provenance and capture metadata
- Temporary unresolved results

Out of scope:

- Recognition job orchestration
- Manual intake UI
- Recommendation scoring

## Design

Use provider adapters to normalize external metadata into the internal work/edition model.

### Provider abstraction

Use an abstraction such as `IBookMetadataProvider` so new providers can be added without changing core business rules.

### Canonical import

Promote confirmed metadata into canonical catalog records when a purchase or wishlist flow requires it.

## Behavior

- The backend can query at least one external book metadata provider by ISBN and by title
- The backend can normalize external metadata into the internal work/edition model
- The backend does not treat external metadata as authoritative until it has been confirmed or reconciled

## Testing

Add coverage for:

- ISBN lookup
- title lookup
- canonical import
- preserving provider provenance