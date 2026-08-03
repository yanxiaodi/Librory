# 2026-08-03 Story 12 External Metadata Import

- Added `POST /api/book-metadata/import` so signed-in users can promote one normalized metadata candidate into the canonical catalog.
- Introduced application and infrastructure import abstractions that reuse the existing `BookWork` and `BookEdition` model.
- Implemented exact-ISBN reuse so a matching edition returns the existing canonical work instead of creating a duplicate.
- Preserved source provenance and capture time on imported work and edition metadata fields.
- Documented the import endpoint in the API reference and marked story-12 as delivered in the backend story map.
- Verified the full `Librory.Api.Tests` suite passes after the change.
