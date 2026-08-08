# Story 15 Design: Recommendation Profile Web

## Goal

Add a mobile-first reading preferences card to Settings so members can edit their recommendation profile from the web app.

## Scope

In scope:

- Typed frontend client for profile GET/PUT endpoints
- Reading preferences card in Settings
- Admin member switching
- Empty-state handling for missing profiles

Out of scope:

- Recommendation scoring logic
- AI workflows
- Family invitation management

## Design

The settings page should expose the profile as a simple form, not a separate workflow.

### Controls

- age range
- favorite and excluded authors
- genre and style
- language
- notes
- visibility
- family recommendation toggle

### Behavior

- Missing profile should render as an empty form
- Explicit `null` and `[]` values should clear fields
- Forbidden responses should remain read-only

## Testing

Add coverage for:

- loading the current member profile
- saving edits
- preserving clear semantics
- handling forbidden responses