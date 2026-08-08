# Story 06 Design: Recommendation Profiles

## Goal

Store manual reading preferences and return recommendation inputs that the UI and AI workflow can use.

## Scope

In scope:

- Recommendation profile persistence
- Member-scoped profile editing
- Curated genre and style choices

Out of scope:

- Automatic preference learning
- AI orchestration
- Scan result ranking changes

## Design

Keep recommendation profiles as explicit user-managed data.

### Profile model

The profile should support age range, favorite authors, favorite genres, and favorite styles.

### Permissions

Regular members can edit their own profile, and admins can switch between member profiles.

## Behavior

- Profiles are stored per member
- Partial updates preserve existing values
- Invalid age ranges are rejected
- Favorite lists are trimmed and deduplicated case-insensitively

## Testing

Add coverage for:

- creating and updating a profile
- preserving partial edits
- enforcing member visibility rules