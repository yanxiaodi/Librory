# Story 15 Design: Recommendation Profile Management Web

## Goal

Add a mobile-first Settings experience for managing the recommendation profile attached to the current family member. Administrators can manage profiles for active members in the selected family; regular members can manage their own profile.

## Scope

This web slice consumes the Story 15 member-scoped profile API:

- `GET /api/family/current/members/{memberId}/recommendation-profile`
- `PUT /api/family/current/members/{memberId}/recommendation-profile`

The existing current-member aliases remain available but are not required by the new component. This slice does not select scan targets, infer scan language, score books, or call AI workflows; those remain Story 16 and Story 09 work.

## User experience

Add a `Reading preferences` card to the existing Settings page, using the current card, input, button, spacing, and theme-token patterns.

- Select the current active member by default.
- Administrators can switch the selected member to any active family member.
- Regular members can edit only their own profile. If they select or encounter a profile they cannot edit, show a clear read-only/permission message and do not render editable controls.
- A missing profile (`404`) displays an empty form and allows the first save to create it.
- Loading, save, and API error states are visible and do not lose the current form values.

The form contains:

- minimum and maximum reading age;
- favorite and excluded authors, genres, and styles;
- preferred book languages;
- preference notes;
- profile visibility (`Family` or `Private`);
- whether the profile may be used for family recommendations.

Authors, genres, and styles use comma-separated text inputs for this first slice. Preferred languages use a multi-select or checkbox group based on the existing `PreferredLanguage` values. Empty collection inputs save as `[]`; cleared nullable scalar values save as `null`. The form submits the complete current state so an intentional clear is distinguishable from an omitted field.

## Frontend boundaries

Add typed profile contracts and GET/PUT helpers to `src/Librory.Web/src/lib/familyApi.ts`. Keep profile form state and API mapping inside a focused family/recommendation component rather than adding profile-specific logic to `SettingsPage`.

The member list type should include the Story 15 metadata already returned by the API: profile availability, visibility, and family-recommendation usability. The UI must not infer permission from these metadata fields; edit capability remains enforced by the API and the current session role.

## Error handling and privacy

- Treat `404` as “no profile yet,” not as a page error.
- Treat `403` as read-only access and avoid exposing private notes.
- Show a concise retryable error for other failures.
- Never display private notes from a failed or unauthorized response.

## Testing and validation

Add focused tests for:

1. loading the current member's profile into the form;
2. empty-form behavior after a `404` and successful creation on save;
3. member switching for an administrator;
4. payload mapping for collection clearing and nullable fields;
5. save error and read-only/forbidden states.

Run the frontend lint, test, and build commands before opening the PR. Perform a browser review at mobile and desktop widths to verify the new card matches the existing Settings layout and remains usable when the form is long.
