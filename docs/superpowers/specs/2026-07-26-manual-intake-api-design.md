# Story 03b Design: Manual Intake API

## Goal

Expose the existing manual intake application flow as a family-scoped API so a signed-in member can create a `BookCopy` for a resolved `BookEdition` without introducing any ISBN lookup or front-end work yet.

## Scope

In scope:

- `POST /api/family/current/book-copies`
- `GET /api/family/current/book-copies/{bookCopyId}`
- Request and response contracts for manual intake
- Duplicate detection summary returned alongside the created copy
- API integration tests and API reference updates

Out of scope:

- Front-end pages or UI routing
- External metadata lookup providers
- ISBN/title search against third-party services
- Persistence schema changes
- Recommendation refresh orchestration beyond the existing duplicate summary

## Design

Add a small family-scoped book-copy resource under the current-family API group.

### Create flow

The `POST` endpoint accepts:

- `bookEditionId` as the resolved edition to attach
- optional purchase metadata
- optional intake notes
- an optional duplicate status, defaulting to `Unchecked`

The endpoint resolves the current family and current member from the auth context, loads the requested edition, and passes the loaded family, current member, and edition into the existing `ManualBookIntakeRecorder.RecordWithDuplicateDetection(...)` helper.

The response should return:

- the created copy
- whether the family already appears to own a duplicate
- the follow-up warning text when duplicates are present

### Fetch flow

The `GET` endpoint returns a single copy for the current family by id. It exists so the create response can point at a stable resource location and so later UI work has a direct fetch route.

### Contracts

Recommended request shape:

- `BookEditionId`
- `DuplicateStatus` with a default of `Unchecked`
- `Condition`
- `PurchaseStore`
- `PurchasePrice`
- `ShelfLocation`
- `PurchasedAt`
- `IntakeNotes`

Recommended response shape:

- `BookCopyId`
- `FamilyId`
- `MemberId`
- `BookEditionId`
- `DuplicateStatus`
- `Condition`
- `PurchaseStore`
- `PurchasePrice`
- `ShelfLocation`
- `PurchasedAt`
- `IntakeNotes`
- `HasPotentialDuplicate`
- `DuplicateWarning`

## Behavior

- Missing or unknown editions return `404 Not Found`
- Missing auth returns `401 Unauthorized`
- Invalid intake data returns `400 Bad Request`
- The current member is used as the copy owner
- Duplicate detection stays warning-only and does not block save

## Testing

Add API integration coverage for:

- creating a copy from a resolved edition
- reading the created copy back
- returning the duplicate warning when the family already owns the same edition/work
- rejecting a missing edition with `404 Not Found`

## Risks

- The endpoint needs the edition, work, family copy graph, and current member loaded correctly or duplicate detection will not have enough context.
- Keeping the owner tied to the current member is the right minimal slice, but future UI work may want an admin override later.
