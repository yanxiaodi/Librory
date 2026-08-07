# Story 15 member recommendation profiles

## What changed

- Expanded the existing recommendation profile to store excluded preferences, preferred book languages, bounded notes, visibility, and family-recommendation use state.
- Added member-scoped profile GET/PUT endpoints while preserving current-member aliases.
- Added database-backed permission checks for profile owners and active family administrators.
- Added family-member list metadata for scan target selection without exposing private notes.
- Added explicit JSON update semantics: omitted fields are preserved and explicit `null` clears a field.

## Boundary

This story does not select scan targets, infer scan language, score books, or call AI workflows. Those remain Story 16 and Story 09 work.

## Validation

- Domain: 80 tests passed.
- Application: 41 tests passed.
- API: 63 tests passed.
- The migration initializes new JSON list columns with `[]`, visibility with `Family`, and family recommendation use with `true` for existing profiles.
