# Story 16 Design: Scan Context Web

## Goal

Expose scan target selection and language context in the web app so the user can see which member a scan is using before review.

## Scope

In scope:

- Current target member display
- Language context display
- Admin member switching
- Scan context summary in the review flow

Out of scope:

- Recommendation scoring
- AI orchestration
- Family invitation management

## Design

Treat scan context as a small summary panel attached to the scan review flow.

### Behavior

- Show the selected target member
- Show the inferred language context when available
- Keep profile notes private

## Testing

Add coverage for:

- rendering the current scan target
- switching targets as an admin
- preserving privacy boundaries