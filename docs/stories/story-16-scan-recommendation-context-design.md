# Story 16 Design: Scan Recommendation Context

## Goal

Select one target member and infer per-scan language context before recommendation so scan results can be personalized without mutating saved profiles.

## Scope

In scope:

- Family-scoped scan target selection
- Current-member defaulting
- Profile authorization for family-visible members
- Per-scan language inference

Out of scope:

- Recommendation scoring itself
- AI orchestration
- Front-end shell redesign

## Design

The scan context should be a temporary layer that sits between the scan session and later recommendation work.

### Target selection

Choose a target member for the scan, defaulting to the current member when appropriate.

### Language context

Infer a temporary language context from the scan without mutating the saved recommendation profile.

## Behavior

- The selected target should be visible in scan responses
- The inferred language should be temporary
- Saved profile notes should remain private

## Testing

Add coverage for:

- selecting the current member by default
- preserving profile privacy
- handling mixed-language scans