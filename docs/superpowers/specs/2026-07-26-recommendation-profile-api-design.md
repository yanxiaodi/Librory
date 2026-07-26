# Story 06b Design: Recommendation Profile API

## Goal

Expose the existing per-member recommendation profile model as a family-scoped API so the current signed-in member can create, update, and read their own recommendation preferences.

## Scope

In scope:

- `GET /api/family/current/recommendation-profile`
- `PUT /api/family/current/recommendation-profile`
- Request and response contracts for recommendation profiles
- Persistence mapping for `RecommendationProfile`
- API integration tests and API reference updates

Out of scope:

- Front-end pages
- Recommendation scoring or ranking logic
- AI recommendation workflows
- Any cross-member or family-shared recommendation profile

## Design

Treat recommendation preferences as a singular current-member resource.

### Read flow

The `GET` endpoint returns the recommendation profile for the current signed-in member when one exists. If the member has not created a profile yet, return `404 Not Found`.

### Upsert flow

The `PUT` endpoint creates the profile if it does not already exist, or updates the existing profile in place if it does. The endpoint should:

- resolve the current family and member from auth
- load the current member from the database
- create or update the member's profile
- preserve existing values when the request omits them
- let the domain continue enforcing age-range validation and value normalization

### Persistence

Add `RecommendationProfile` to the EF model as a real persisted entity.

Recommended storage shape:

- one profile per member
- `member_id` as a unique foreign key
- favorite authors / genres / styles stored in JSON-friendly columns with list semantics preserved

This keeps the API simple and matches the domain rule that a member owns one profile.

## Behavior

- Missing auth returns `401 Unauthorized`
- Missing member or family context returns `404 Not Found` or `401 Unauthorized` as appropriate
- Invalid age ranges still surface as validation errors from the domain
- Empty or partial update requests preserve existing profile fields where possible

## Testing

Add API integration coverage for:

- creating a recommendation profile for the current member
- reading the saved profile back
- updating the same profile without creating a second record
- preserving existing preference values across partial updates
- rejecting invalid age ranges

## Risks

- The profile lists need a stable EF conversion so changes persist correctly.
- The API should stay member-scoped; adding family-shared recommendation settings later would need a separate model.
- JSON-friendly list columns are fine for this slice, but they are not ideal if future work needs server-side author or genre filtering. Revisit the storage type if those queries become important.
