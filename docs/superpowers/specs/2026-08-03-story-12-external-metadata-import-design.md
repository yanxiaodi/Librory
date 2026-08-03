# Story 12 Design: External Metadata Providers and Canonical Import

## Goal

Let a signed-in user search an external book metadata provider by title, review the normalized results, and confirm one result into Librory's canonical catalog as a `BookWork` with an optional first `BookEdition`.

## Scope

In scope:

- External metadata search by title through the existing provider abstraction
- A confirm-and-import API that creates canonical catalog records from a normalized metadata candidate
- Canonical import that reuses the current `BookWork` and `BookEdition` model
- Provenance preservation for imported metadata fields
- API and integration tests for search and import

Out of scope:

- Re-indexing or periodic sync of already imported catalog data
- Multiple-provider routing or provider selection UI
- Import session state or draft buffers
- Recommendation scoring
- Shelf photo recognition
- Manual intake of a purchased copy

## Recommended Approach

Keep the provider boundary provider-neutral, but keep the implementation simple:

- `GET /api/book-metadata/search` continues to return normalized external metadata candidates.
- `POST /api/book-metadata/import` accepts one normalized candidate and turns it into canonical catalog data.
- The import path reuses the same canonical book rules already used by scan-candidate resolution and `BookWork` creation.

Why this is the right shape:

- It keeps the API small and easy to reason about.
- It avoids introducing a separate import-session subsystem just to remember one selected result.
- It aligns canonical import with the existing `BookWork -> BookEdition` model instead of creating a parallel book shape.
- It leaves room for later provider expansion without changing the request/response contract.

Rejected alternatives:

- A persistent import session would add state we do not need yet.
- A synchronous search-plus-import combined endpoint would blur the confirm step.
- A provider-specific import endpoint would hard-code Google Books into the API surface.

## API Design

### Search

Keep the existing anonymous search endpoint:

- `GET /api/book-metadata/search?title=...&language=...&maxResults=...`

This endpoint stays read-only and returns `BookMetadataSearchResponse`.

### Import

Add an authenticated import endpoint:

- `POST /api/book-metadata/import`

Request body:

```json
{
  "candidate": {
    "source": "GoogleBooks",
    "sourceId": "some-provider-id",
    "title": "Dune",
    "subtitle": null,
    "authors": ["Frank Herbert"],
    "publisher": "Ace",
    "publishedDate": "1965",
    "language": "en",
    "description": "A science fiction novel...",
    "isbn10": "0441013597",
    "isbn13": "9780441013593",
    "thumbnailUrl": "https://...",
    "infoUrl": "https://..."
  }
}
```

Response:

- `201 Created` when a new canonical work or edition is created
- `200 OK` when the request matches an already-existing canonical record and the service reuses it
- The response payload is the existing `BookWorkResponse`
- The `Location` header points to `/api/book-works/{bookWorkId}`

The import endpoint should validate:

- `candidate.title` is present
- `candidate.source` is present
- `candidate.sourceId` is present
- `candidate.authors` may be empty, but blank strings must be rejected

The import endpoint should not require family context. It is catalog-level work, not family-owned inventory.

## Canonical Import Model

The import service should map the normalized external candidate into the current canonical book model:

- `candidate.title` becomes `BookWork.CanonicalTitle`
- the candidate author list becomes the canonical author string
- `candidate.description` becomes `BookWork.Summary`
- `candidate.subtitle` becomes `BookEdition.Subtitle`
- `candidate.isbn10` or `candidate.isbn13` becomes `BookEdition.Isbn`
- `candidate.publishedDate` becomes `BookEdition.PublicationYear` when a year can be parsed

Field handling rules:

- Prefer `isbn13` over `isbn10` when both are present.
- Use the provider author list in display order and join it into the canonical author string.
- Parse a four-digit publication year from the provider date when possible.
- Store provider source, source id, and capture time in `MetadataProvenance`.
- Do not store raw provider JSON.
- Do not store provider fields that the canonical model cannot represent yet, such as publisher, thumbnail, or info URL.

If an exact ISBN match already exists in the canonical catalog, the import service should reuse the existing edition and its parent work instead of creating a duplicate.

If no ISBN is present, the import service should create a new work and only create a first edition when edition-level data is available.

## Data Flow

1. The user searches external metadata by title.
2. The provider abstraction returns normalized candidates.
3. The frontend shows the returned candidates and the user selects one.
4. The frontend posts the selected candidate to `POST /api/book-metadata/import`.
5. The import service checks for an exact ISBN match in the canonical catalog.
6. If a match exists, the service returns the existing `BookWorkResponse`.
7. If no match exists, the service creates a new `BookWork` and optional first `BookEdition`.
8. The API returns the canonical work payload so downstream flows can reuse it immediately.

## Error Handling

- Missing auth on import returns `401 Unauthorized`.
- Missing or blank `candidate.title` returns `400 Bad Request`.
- Missing or blank `candidate.source` or `candidate.sourceId` returns `400 Bad Request`.
- Invalid or oversized request bodies return `400 Bad Request`.
- Provider search failures continue to surface as `502 Bad Gateway` from the existing search endpoint.
- Import persistence failures return `500 Internal Server Error` with a generic error message.

The import path should prefer validation errors over server errors when the selected candidate is malformed.

## Testing

Add coverage for:

- searching metadata by title through the existing provider abstraction
- returning normalized candidate fields instead of raw provider JSON
- importing a selected candidate into a new `BookWork`
- importing a candidate into a `BookWork` plus first `BookEdition` when ISBN or publication data is present
- reusing an existing canonical work or edition when the ISBN already exists
- rejecting blank or malformed import requests

The tests should verify canonical responses, not provider-specific transport details.

## Dependencies

This story depends on:

- the existing `GET /api/book-metadata/search` endpoint
- the Google Books metadata provider implementation
- the `BookWork` and `BookEdition` canonical model
- the existing family-authenticated API surface

It does not depend on scan recognition jobs, recommendation scoring, or any later sync workflow.

## Story Boundary

This story is complete when:

- a signed-in user can search external metadata by title,
- the frontend can select one normalized result,
- the server can import that result into the canonical catalog,
- the import returns a canonical work payload,
- and the workflow stops after canonical import without introducing import sessions or periodic synchronization.
