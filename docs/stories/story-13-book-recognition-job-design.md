# Story 13 Design: Book Recognition Job

## Goal

Accept a shelf or book-cover photo, process recognition asynchronously, and return ranked book-title candidates enriched with external metadata.

## Scope

In scope:

- Temporary image storage for uploaded photos
- OCR-first title extraction with an optional vision fallback
- Candidate ranking and noise reduction
- Metadata lookup for the strongest title candidates
- Pollable job states

Out of scope:

- Recommendation scoring
- Candidate correction
- Canonical import
- Manual intake

## Design

Keep recognition asynchronous and focused on candidate generation.

### Delivered scope

The job should return ranked candidates and metadata, not permanent library changes.

### Follow-up boundary

Recommendation scoring, candidate correction, canonical import, and manual intake remain explicit follow-on steps.

## Behavior

- The API accepts a photo and returns a job id immediately
- The frontend can poll until the result is ready
- The result includes ranked candidates and metadata

## Testing

Add coverage for:

- queued and running states
- succeeded and failed states
- ranked candidate output
- metadata enrichment