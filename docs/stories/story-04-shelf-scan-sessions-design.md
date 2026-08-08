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

## Implementation Status

This story is implemented in the current branch and is the active delivery slice.

The existing scan flow covers the recognition job handoff, the temporary scan session API, and the web review surface. The remaining work should stay outside this story boundary and move to manual intake or duplicate detection follow-on stories.

## Next Step

Move to the next story after this slice, keeping manual intake and duplicate detection as separate follow-on stories.