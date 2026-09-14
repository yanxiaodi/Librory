# Scan-to-Purchase Recommendation Flow

## Goal

Complete the MVP shopping journey from a shelf photo to purchased books saved in the family library:

1. Capture a shelf photo.
2. Recognize and enrich multiple book candidates.
3. Show recognition match quality and duplicate warnings.
4. Let the user correct a candidate and re-search metadata.
5. Let the user choose a metadata match and a family member.
6. Confirm each purchase and save a `BookCopy`.
7. Keep the session available so the user can purchase more candidates later.

Wishlist and AI personalization are separate follow-up slices. The first slice must complete the reliable scan-to-library workflow without pretending that recognition confidence is a recommendation score.

## Current Starting Point

The repository already contains:

- asynchronous shelf-photo recognition;
- Google Books metadata enrichment and re-ranking;
- scan target-member and language-context persistence;
- member recommendation profiles and Settings UI;
- family-wide duplicate detection;
- canonical metadata import;
- manual book intake through `ManualBookIntakeRecorder`;
- a seven-day default retention window for temporary scan sessions;
- a Web polling flow for recognition jobs.

The current scan UI derives a persisted `RecommendationScore` from recognition rank, and the current book-copy endpoint assigns ownership to the signed-in member. This slice separates recognition match quality from future recommendation scoring and adds a purchase flow with explicit ownership and purchase actor.

## First-Slice Decisions

### Recognition quality and recommendation score are separate

The first slice exposes recognition information only:

- `RecognitionRank` or `RecognitionConfidence` describes how reliable recognition and metadata matching are;
- `RecommendationScore` is absent/null until AI personalization exists;
- `DuplicateWarning` is an independent family-library safety signal.

The UI must not label recognition rank as a recommendation score. A missing or empty recommendation profile does not block scanning or purchasing; it means that no personalized score is shown.

### One session contains many purchases

A scan session may contain many candidates. The user may purchase any number of them, one at a time. Each candidate represents one intended copy in the first slice:

- a candidate can be purchased once;
- buying a second copy requires a new scan or manual intake;
- other candidates remain available after one purchase;
- purchased candidates remain in the session as read-only records.

The Home page and Scans page use the latest non-expired family scan session as the continuation entry point. Sessions retain their original expiration time; a purchase does not extend it. The default retention period remains seven days. Expiration removes temporary scan data and photos, never already-created book copies.

### Purchase ownership and actor

The purchase flow records two distinct roles:

- `OwningMemberId`: the family member the book is for;
- `PurchasedByMemberId`: the active member executing the purchase/intake action.

The owner may be any member of the current family, including a deactivated member. The caller must be an authenticated active member of the current family. The owner id must be validated against the same family and may not be supplied from another family. `PurchasedByMemberId` is derived from the authenticated current member and cannot be supplied by the client. There is no separate buyer-versus-recorder distinction in this slice.

The owner selector defaults to the scan target but permits any family member. The scan target itself remains subject to the existing active-member and recommendation-eligibility rules.

### Purchase metadata

Required purchase inputs:

- selected metadata match or manual title/author data;
- `OwningMemberId`;
- duplicate status when a duplicate warning exists.

Optional inputs:

- purchase time, defaulting to the current time but editable;
- store name;
- price;
- condition;
- shelf location;
- intake notes.

Purchase location is intentionally deferred. Existing optional purchase fields should remain available where they already exist.

## Recommendation and AI Boundary

### First slice

Do not calculate or display personalized recommendation scores. Sort candidates by recognition match quality and show the recognition evidence, metadata confidence, and duplicate warning.

If a member has no usable preference configuration, the flow remains usable and shows a setup prompt rather than a fake score.

### Future AI slice

AI personalization is an asynchronous enhancement:

1. Basic scan results appear immediately.
2. The API starts a recommendation job after the basic results are available.
3. The Web client polls the job over HTTP, reusing the existing recognition-job pattern.
4. Completed results are merged into React state by `candidateId`.
5. The server persists the result so reopening the scan session restores it.
6. AI failure leaves the basic scan and purchase flow usable.

AI results must include and validate:

- candidate version;
- recommendation-profile version;
- private-note authorization state;
- model/prompt version.

Results for changed, deleted, purchased, or expired candidates are stale and must not overwrite current state. Results must not forcibly reorder candidates after the user has started editing, deleting, or purchasing; a later explicit sort action may apply AI ordering.

The first AI job targets one member per scan session. Multi-member aggregate recommendations are out of scope.

### Private preference notes

Add an explicit `UsePrivateNotesInFamilyRecommendations` consent field now, defaulting to false, but do not send private notes to AI in the first slice.

When AI is later implemented:

- only the profile owner can read, enable, or disable this consent;
- administrators cannot view or change the note or consent;
- other members may select a target only when the target has opted into family recommendations;
- the API may send authorized notes to AI without returning them to the current user;
- AI output must not quote or expose the note content.

Structured preference use remains separately controlled by `UseInFamilyRecommendations`.

## Purchase Confirmation API

Add a family-scoped confirmation endpoint:

```text
POST /api/family/current/scan-sessions/{scanSessionId}/candidates/{candidateId}/purchase
```

The request contains:

- a stable `purchaseRequestId` for idempotent retries;
- the selected normalized metadata candidate, or manual metadata input;
- optional edition fields (`isbn`, `format`, `publicationYear`);
- `ownerMemberId`;
- duplicate-status confirmation when needed;
- optional purchase/intake fields.

The endpoint performs one server-owned business operation:

1. Resolve the current family context and require an active caller.
2. Load the non-expired scan session and candidate within that family.
3. Return the previous result if the same candidate and `purchaseRequestId` were already processed.
4. Validate the owner belongs to the current family; do not require the owner to be active.
5. Import or reuse the selected canonical work and edition.
6. Re-run family-wide duplicate detection against the resolved edition.
7. Apply the duplicate status rule.
8. Create the `BookCopy` with both owner and current purchaser.
9. Mark the candidate purchased and associate its `BookCopyId`.
10. Commit and return the created copy, canonical book information, and duplicate result.

The endpoint is the only authority for family isolation, ownership, duplicate detection, canonical import, and purchase state. The client may propose metadata and owner selection, but may not decide the purchaser, duplicate result, or family membership.

## Duplicate Handling

The existing statuses are `Unchecked`, `ConfirmedUnique`, and `ConfirmedDuplicate`.

- If no duplicate is found, the server records `ConfirmedUnique` automatically;
- if a duplicate is found, the UI requires an explicit decision and records `ConfirmedDuplicate` when the user proceeds;
- the purchase endpoint does not accept `Unchecked` for a completed purchase;
- duplicate detection is warning-only and never silently blocks purchase.

When a duplicate warning appears, the user chooses one of:

1. same existing edition: create another copy against the existing edition;
2. same work, different edition: reuse the existing work and create a new edition;
3. not the same book: create a new work and edition.

This prevents no-ISBN purchases from creating unnecessary duplicate canonical works.

## Metadata and Edition Handling

### Metadata snapshot

Persist the normalized metadata-match snapshot on each scan candidate in a PostgreSQL `jsonb` column. The snapshot is temporary session data and is not queried by nested fields. It must include provider/source id, title, authors, publication information, ISBNs, cover/link fields, and enough information to reproduce the original selection after reopening the session.

Use an explicit DTO/schema and stable JSON serialization. Add migration and read/write round-trip coverage.

### Version-unconfirmed editions

An edition may be created with incomplete version information. Add explicit `BookEdition.IsProvisional` state instead of inferring provisional status from null ISBN/format/year.

- `IsProvisional = true` permits fast intake when the version cannot be confirmed;
- the UI clearly labels the edition as unconfirmed;
- users can later fill ISBN, format, or publication year and clear the provisional state;
- duplicate detection still runs using the available title and edition signals.

The metadata import operation must support creating a provisional edition for this purchase path. A normal provider import must not accidentally create an editionless book when a copy is about to be created.

### Provider failure and manual fallback

If the metadata provider is unavailable or returns no match, the user may manually enter title/author and create a `source = Manual` provisional work/edition. Provider-supplied fields keep provider provenance; user-entered or corrected fields use `Manual` provenance and must not be silently overwritten by later provider refreshes.

## PostgreSQL Transaction and Retry Rules

The purchase service owns the outer transaction. Metadata import must not start or commit an independent nested transaction.

- Use one `DbContext` and one Npgsql transaction for import, duplicate evaluation, copy creation, and candidate-state update;
- `SaveChangesAsync` persists within the transaction but does not commit it;
- use PostgreSQL-compatible constraints/conflict handling for ISBN reuse;
- on serialization failure (`40001`) or deadlock (`40P01`), discard the failed transaction and retry the whole operation with a new transaction/context up to two times;
- never continue using a failed transaction;
- after retries are exhausted, return a retryable error without a partial `BookCopy`.

The transaction must cover canonical import and copy creation together. A failed intake must not leave a newly-created canonical work/edition from the same purchase attempt.

## Candidate Purchase State

Do not delete purchased candidates immediately. Persist purchase state on `ScanCandidate`:

- `PurchaseStatus` (`Pending` or `Purchased`);
- `PurchasedBookCopyId`;
- `PurchaseRequestId`;
- `PurchasedAt`.

The same candidate and request retry returns the existing purchase result. A purchased candidate is read-only in the session: it cannot be purchased again or discarded. Session cleanup may later remove the temporary candidate without affecting the book copy.

## Web Flow

Extend the existing scan review surface.

For each candidate, display:

- recognition match quality and evidence;
- metadata matches with the highest-ranked match selected by default;
- title, author, publication year, ISBN, cover/link where available;
- duplicate warning and follow-up hint;
- pending, purchased, or version-unconfirmed state;
- purchase action for pending candidates.

The user may edit a candidate title and explicitly re-search metadata without re-uploading the shelf photo. New provider results replace the candidate's metadata snapshot and reset stale duplicate/review state.

The purchase interaction selects an owner, defaults purchase time to now, and exposes optional intake fields without making them required. On success, the candidate becomes read-only and the UI shows the created book copy and owner. Errors preserve the selected metadata and form values.

When AI results later arrive, the client merges them by candidate id. It may reorder untouched candidates, but must not forcibly move candidates after the user has edited, deleted, or purchased one.

## Session Continuation

- Home exposes “Continue latest scan” when a non-expired session exists;
- Scans opens the latest non-expired session when appropriate;
- the session response contains all pending and purchased candidates;
- the seven-day expiration is not extended by purchases;
- an expired session returns not found and its temporary photo/candidates are cleaned up;
- existing `BookCopy` records remain available in the family library.

## API and Domain Boundaries

- Keep `ManualBookIntakeRecorder` as the domain/application entry point for creating copies;
- add explicit purchaser and owner handling to all new `BookCopy` creation paths;
- existing historical copies may have a null purchaser until backfilled;
- extract or reuse a canonical metadata-import operation that can participate in the caller-owned transaction;
- add dedicated purchase request/response contracts;
- keep recognition quality, future recommendation scoring, duplicate detection, metadata import, and purchase state as separate concepts;
- preserve provenance for provider and manual fields;
- do not expose persistence entities directly from the API.

## Failure Handling

- no authenticated active caller: `401 Unauthorized`;
- scan session/candidate outside the current family or expired: `404 Not Found`;
- owner outside the current family: family-isolated validation/not-found response;
- duplicate found: explicit user decision required, but purchase remains allowed;
- missing edition data: allow provisional edition, not an editionless `BookCopy`;
- provider unavailable: manual provisional metadata fallback;
- metadata/import/intake failure: transaction rollback and retryable error;
- stale candidate/profile/consent version: do not apply stale AI results;
- repeated purchase request: return the existing result;
- UI disables the submit action while saving, but server-side idempotency remains authoritative.

## Validation

### Domain/application tests

- recognition quality is separate from recommendation score;
- no profile produces no personalized score but does not block purchase;
- deterministic recognition ordering and bounds;
- purchase state transitions from pending to purchased once;
- duplicate status auto-confirms unique candidates and requires confirmation for duplicates;
- provisional edition can later become confirmed;
- manual provenance is preserved for user corrections;
- purchaser and owner are distinct members when appropriate.

### API integration tests

- active caller can purchase for another active member;
- active caller can purchase for a deactivated member in the same family;
- admin and non-admin callers follow the same owner rule;
- foreign-family owners and unauthenticated callers are rejected;
- same candidate supports multiple different purchases only by rejecting the second purchase;
- repeated request returns the original result without a second copy;
- metadata matches survive session reload through JSONB;
- selected metadata is imported/reused and the copy is created atomically;
- same-edition, new-edition, and new-work duplicate branches work;
- provisional and manual fallback editions create valid copies;
- provider failure and import/intake failure leave no partial purchase;
- serialization/deadlock retry re-runs the whole transaction;
- current purchaser is derived from auth and cannot be spoofed;
- all new copies record purchaser; historical copies remain compatible;
- private-note consent can only be changed/read by the profile owner.

### Web tests

- recognition quality is shown without a recommendation score;
- title editing and metadata re-search update the candidate;
- highest-ranked metadata match is selected by default but can be changed;
- owner selector defaults to the scan target and permits any family member;
- purchase time defaults to now and remains editable;
- duplicate confirmation shows the three canonical-resolution choices;
- successful purchase makes only that candidate read-only;
- other candidates remain purchasable;
- session reload restores metadata matches and purchase states;
- repeated submission does not duplicate the copy;
- validation and server errors preserve form state;
- future AI updates merge by candidate id without overwriting user changes.

### Manual verification

Run a representative mobile shelf photo through the full flow:

1. recognize several candidates;
2. correct one title and re-search metadata;
3. buy several different candidates in one session;
4. assign one to another active member and one to a deactivated member;
5. confirm a duplicate and select each relevant resolution branch;
6. create one provisional/manual edition;
7. reload from Home and continue the session;
8. verify the resulting copies, owners, purchaser, duplicate statuses, and provenance in the family library.

## Out of Scope

- Wishlist UI or wishlist conversion;
- batch checkout or quantities greater than one per scan candidate;
- purchase location, city, address, or GPS fields;
- automatic preference learning;
- AI as the authoritative blocking mechanism;
- multi-member aggregate recommendations;
- full genre/style/age classification before the base flow is working;
- redesign of family invitations or authentication.
