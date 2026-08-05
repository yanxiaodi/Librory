# Story 15 Design: Member Recommendation Profiles

## Goal

Give each family membership a structured, permission-aware recommendation profile that can be maintained by the member or a family administrator and consumed by later recommendation workflows.

## Scope

In scope:

- One profile per active or historical family member.
- Age-range preferences as soft recommendation signals.
- Positive and excluded authors, genres, and styles.
- Preferred book languages, separate from UI language.
- Bounded free-text preference notes for AI context.
- Profile visibility and recommendation-use controls.
- Member-scoped read and update APIs.
- Explicit partial-update and clearing semantics.

Out of scope:

- Standardized reading-level scores.
- Per-preference weights.
- Automatic preference learning.
- Cross-family profile synchronization.
- Field-level privacy controls.

## Profile Model

`RecommendationProfile` belongs to one family membership, not directly to the account and not to the whole family.

Suggested fields:

- `MemberId`
- nullable `MinimumAge`
- nullable `MaximumAge`
- `FavoriteAuthors`
- `ExcludedAuthors`
- `FavoriteGenres`
- `ExcludedGenres`
- `FavoriteStyles`
- `ExcludedStyles`
- `PreferredBookLanguages`
- nullable `PreferenceNotes`, bounded to 1,000 characters
- `ProfileVisibility`: `Family` or `Private`
- `UseInFamilyRecommendations`: boolean
- created and updated timestamps

## Recommendation Semantics

- Age range is a strong ranking signal, not a hard filter.
- A book outside the range can still be recommended when other evidence is strong.
- Excluded authors and genres are stronger negative signals than positive preferences.
- Preferred book language is a ranking signal, not a permanent filter.
- The UI language remains `Member.PreferredLanguage` and is independent from book-language preference.
- `PreferenceNotes` are soft AI context and never override explicit exclusions.
- No profile weights are stored in this story.

## Visibility and Permissions

Profile visibility and recommendation use are separate:

- `ProfileVisibility = Family`: family members can see structured preferences and use the profile.
- `ProfileVisibility = Private`: only the profile owner and family administrators can see full profile details or use the profile for another member's scan.
- `UseInFamilyRecommendations = false`: the profile cannot be selected for another member's scan.
- Profile owners and family administrators can create, update, and clear profiles.
- Other family members may select an allowed family-visible profile for recommendation without gaining edit access.
- `PreferenceNotes` remain visible only to the owner and administrators in this first version.

## API Direction

Keep the current-member endpoints as convenience aliases, and add member-scoped endpoints:

- `GET /api/family/current/members/{memberId}/recommendation-profile`
- `PUT /api/family/current/members/{memberId}/recommendation-profile`

The member list response should expose enough information for target selection without leaking private notes: member id, display name, active state, profile availability, visibility, and whether the profile is usable for family recommendations.

Update semantics:

- omitted field: preserve the existing value
- explicit `null`: clear the field
- supplied value: replace the field
- invalid age range: reject
- notes over the maximum length: reject
- inactive or foreign member: reject

## Acceptance Criteria

- Each family member has at most one recommendation profile.
- A profile can be created, read, partially updated, and cleared by the owner or an administrator.
- A normal member cannot edit another member's profile.
- A normal member can use another member's profile only when it is family-visible and enabled for family recommendations.
- UI language and preferred book language are stored and returned independently.
- Age-range validation and preference normalization remain enforced by the domain.
- Excluded preferences are persisted separately from positive preferences.
- Profile visibility does not expose private notes to unauthorized members.
- Profile data is isolated between memberships in different families, even for the same account.
- The API returns a stable member-scoped shape suitable for scan target selection.
