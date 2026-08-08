# Story 05 Design: Duplicate Detection

## Goal

Check the family library for already-owned books and surface warnings during scan and intake.

## Scope

In scope:

- Family-scoped duplicate checks
- Normalized title matching
- Warning-only duplicate output
- Edition hints in duplicate results

Out of scope:

- Hard blocking saves
- Recommendation scoring
- Catalog import behavior

## Design

Duplicate detection should be reusable across scan review and manual intake.

### Matching behavior

Treat normalized title matches as suspected duplicates, ignoring capitalization, whitespace, and punctuation.

### Output behavior

Return a warning rather than a hard block, and include edition hints when the system has enough information.

## Behavior

- Duplicate checks run against the whole family library
- Duplicate warnings appear during shelf scanning and intake
- The warning can suggest ISBN or barcode capture when title matching is not enough

## Testing

Add coverage for:

- family-scoped duplicate checks
- normalized title matching
- warning-only output
- edition hint output