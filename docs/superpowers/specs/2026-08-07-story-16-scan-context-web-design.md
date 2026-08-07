# Story 16 Design: Scan Context Web Flow

## Goal

Allow a user to choose the family member a shelf scan is for, then persist and display the scan recommendation context produced by the Story 16 API.

## Scope

In scope:

- Load active family members on the scan page.
- Default the target to the current signed-in member.
- Allow one target member per scan.
- Keep the existing asynchronous book-recognition upload and polling flow.
- After recognition completes, create a scan session with the selected target and recognized candidates.
- Display the selected member, profile availability/use, inferred language, and mixed-language state.
- Preserve recognized results when scan-session persistence fails and offer a retry.

Out of scope:

- Selecting multiple members for one scan.
- Changing the selected target after the scan session is created.
- AI recommendation generation.
- Editing or resolving scan candidates.
- Changing recommendation profiles from the scan page.
- Extending the multipart upload endpoint.

## User Flow

1. The scan page loads the current family members.
2. The current member is selected by default.
3. The user may choose another eligible active member.
4. The user uploads a shelf photo and the existing recognition job is created and polled.
5. When recognition succeeds, the client maps recognized candidates into `CreateScanSessionRequest` and sends `targetMemberId`.
6. The response is stored as the scan context and shown above the recognition results.
7. If member loading fails, the current member remains available as the fallback target.
8. If scan-session creation fails, recognition results remain visible and the user can retry persistence.

## Eligibility and Data Mapping

The member selector uses `GET /api/family/current/members`. It includes active members that are either the current member or have `canUseForFamilyRecommendations` set to true. The current member is always retained as a fallback option.

Recognition candidates map as follows:

- `displayTitle` -> `displayTitle`
- first metadata author -> `author`
- recognition rank normalized to `recommendationScore`
- recognition evidence -> `confidenceLabel`
- metadata language `en`/`zh` -> the corresponding scan candidate language enum; other values remain unknown

The session response is the source of truth for target display name, profile flags, inferred language, and mixed-language state. Private profile notes are never requested or rendered.

## Component and API Boundaries

- Extend `familyApi` only with the existing member list type and fetch path if needed.
- Extend `scansApi` with typed scan-session request/response models and a `createScanSession` function.
- Keep recognition job transport in `bookRecognitionApi`.
- Keep scan orchestration and UI state in `ScansPage` for this focused slice; extract a component only for the target selector or context card if it improves testability.

## Error Handling

- Member list errors show a non-blocking message and leave the current member selected.
- A missing or ineligible selected member is corrected to the current member when member data refreshes.
- Recognition errors retain the existing error behavior.
- Scan-session persistence errors show an explicit retry action without discarding the recognition job or candidates.

## Testing

- API unit coverage verifies the scan-session request includes the selected target and mapped candidates.
- `ScansPage` coverage verifies current-member defaulting, alternate-member selection, member-loading fallback, session-context rendering, and persistence retry.
- Existing recognition upload/polling and failure tests remain passing.
- Run the web test suite, lint, and production build before completion.
