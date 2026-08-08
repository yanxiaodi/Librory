# Story 15 Design: Member Recommendation Profiles

## Goal

Store family-scoped, permission-aware reading preferences for each member so recommendation inputs can be personalized without automatic learning.

## Scope

In scope:

- Recommendation profile persistence
- Member-scoped profile editing
- Admin visibility into member profiles
- Curated genre and style choices

Out of scope:

- Automatic preference learning
- AI orchestration
- Scan result ranking changes

## Design

Keep recommendation profiles as explicit user-managed data.

### Profile model

The profile should support:

- age range
- favorite and excluded authors
- genre and style preferences
- language preference
- notes and visibility flags

### Permissions

- Regular members can edit their own profile
- Admins can switch between member profiles
- Forbidden access should not leak private profile fields

## Testing

Add coverage for:

- creating and updating a profile
- preserving partial edits
- enforcing member visibility rules