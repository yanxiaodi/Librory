# Story 02c Design: Book Metadata and Inference Fields

## Goal

Add the smallest useful metadata layer to the book domain model so later search, intake, localization, and enrichment work can carry source facts and inferred values without reshaping the model again.

## Scope

In scope:

- A shared provenance value object for book metadata
- Metadata fields on `BookWork`
- Metadata fields on `BookEdition`
- Domain tests for stored values and guard/default behavior

Out of scope:

- Persistence mapping
- Application service changes
- API endpoint changes
- `BookCopy` metadata
- Search/indexing logic

## Design

Introduce a shared immutable value object named `MetadataProvenance` to describe where a fact came from and how confident the system is about it.

Recommended shape:

- `Source` as an optional string for the originating system or provider
- `SourceId` as an optional string for the upstream record or identifier
- `Confidence` as an optional decimal between `0` and `1`
- `CapturedAt` as an optional timestamp

Add metadata hooks where the model naturally needs them:

- `BookWork`
  - keep `CanonicalTitle` and `CanonicalAuthor` as the factual work-level fields
  - keep `Summary` as a localized work-level field
  - add `SummaryProvenance`
  - add `CanonicalAuthorProvenance`
- `BookEdition`
  - keep `Isbn`, `Format`, and `PublicationYear` as edition-level facts
  - add `Subtitle` as a localized edition-level field
  - add `SubtitleProvenance`
  - add `PublicationYearProvenance`

This keeps the model aligned with the existing `Work -> Edition -> Copy` split:

- work-level fields describe the abstract book
- edition-level fields describe a specific version
- provenance lives next to the field it explains

## Behavior

- Blank strings should continue to normalize to `null`
- Metadata fields should be optional
- Provenance should be optional
- The model should not require confidence or source data for every record

## Testing

Add domain tests that verify:

- `BookWork` can store localized summary data and provenance together
- `BookEdition` can store localized subtitle data and provenance together
- optional provenance values can remain `null`
- blank string normalization still behaves consistently on new metadata fields

## Risks

- The metadata surface can grow quickly if we add too many fields at once
- Provenance needs to stay generic enough to work for future recognition and enrichment sources
- The model should stay domain-first and not leak persistence concerns into this slice

## Decision

Implement the shared provenance value object first, then wire the smallest set of work-level and edition-level metadata fields around it.
