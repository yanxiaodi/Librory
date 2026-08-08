# Story 04 Design: Shelf Scan Sessions

## Goal

Create temporary shelf scan sessions so users can review results without immediately committing them to the library.

## Scope

In scope:

- Scan session skeleton
- Candidate correction
- Catalog resolution and metadata enrichment
- Scan session API

Out of scope:

- Manual intake
- Duplicate detection logic beyond session-level warnings
- Recommendation scoring changes

## Design

Keep scan sessions temporary and family-scoped.

### Scan session skeleton

Create the temporary scan session record and store the initial batch of candidates for later review.

### Candidate correction

Let a user correct a single scan candidate without restarting the whole shelf session.

### Catalog resolution and metadata enrichment

Promote a scan candidate into a reusable book catalog record when the system has enough evidence or the user explicitly confirms it.

### Scan session API

Expose the temporary scan session workflow as a family-scoped API so the frontend can create, review, correct, resolve, and discard scan data without duplicating domain rules.

## Behavior

- Scan sessions can store multiple candidates
- One candidate can be corrected without replacing the whole session
- Candidates can be promoted into canonical catalog data
- Unresolved candidates remain temporary until promoted or discarded

## Testing

Add coverage for:

- creating a scan session
- storing multiple candidates
- correcting one candidate in place
- promoting a candidate into catalog data
- discarding a candidate from the session