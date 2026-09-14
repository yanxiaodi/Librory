# Scan-to-Purchase Recommendation Flow

## Goal

Complete the MVP shopping journey from a shelf photo to a purchased book saved in the family library:

1. Capture a shelf photo.
2. Recognize and enrich book candidates.
3. Rank candidates for the selected recommendation target.
4. Show recommendation reasons and duplicate warnings.
5. Let the user choose a metadata match and a family member.
6. Confirm the purchase and save a `BookCopy` to the family library.

Wishlist is intentionally outside this slice.

## Current Starting Point

The repository already contains:

- asynchronous shelf-photo recognition;
- Google Books metadata enrichment and re-ranking;
- scan target-member and language context persistence;
- recommendation profiles;
- family-wide duplicate detection;
- canonical metadata import;
- manual book intake through `ManualBookIntakeRecorder`.

The current scan UI still derives a persisted recommendation score from recognition rank, and the current book-copy endpoint always assigns ownership to the signed-in member. This slice replaces the first behavior with a server-side scorer and adds a purchase flow that accepts an explicit family-member owner.

## Design Decisions

### Recommendation scoring is server-owned

Add a focused application-layer scorer rather than calculating scores in `ScansPage`. The scorer receives the selected target profile, the recognition candidate, the chosen metadata match when available, and duplicate context. It returns a bounded score, a label, and structured reasons.

The first scorer uses only signals that are actually present in the normalized metadata contract and recognition result, such as author agreement, preferred book language, recognition confidence/rank, and metadata match quality. Missing signals are neutral rather than guessed. Duplicate warnings remain a separate purchase-safety signal and do not silently reduce recommendation score.

Genre, style, and age-range scoring require additional normalized metadata or classification signals. They should be added as explicit inputs in a later increment, not inferred from arbitrary title text in this slice. AI-generated explanations can be layered on after the deterministic score is stable.

The scorer must be deterministic and independently unit-testable. Weights and thresholds belong in one application component, not in the web client or endpoint handlers.

### Purchase confirmation is one server-side business operation

Add a family-scoped confirmation endpoint:

```text
POST /api/family/current/scan-sessions/{scanSessionId}/candidates/{candidateId}/purchase
```

The request contains:

- the selected normalized metadata candidate;
- `ownerMemberId`;
- duplicate-status confirmation;
- optional condition, store, price, shelf location, purchase date, and intake notes;
- any required edition fields when the selected metadata does not identify an edition.

The endpoint performs the following in one transaction:

1. Resolve the current authenticated family context and require the caller's membership to be active.
2. Load the scan session and candidate within that family.
3. Validate that `ownerMemberId` belongs to the same family. Ownership does not require the owner to be active and does not require the caller to be an administrator.
4. Import or reuse the selected canonical work and edition through the existing metadata-import behavior.
5. Re-run family-wide duplicate detection against the resolved edition.
6. Record the `BookCopy` through the existing manual-intake domain/application path.
7. Return the created copy, canonical book information, and duplicate result.

The duplicate result is a warning, not a hard block. The request must still carry an explicit duplicate status so the user acknowledges the warning. If the selected metadata has no usable edition identity, the API returns a validation response and the UI asks for an ISBN, format, or publication year rather than creating an ambiguous copy.

The server, rather than the client, owns family isolation, owner validation, duplicate detection, and canonical import decisions.

### Ownership and member status

The rules are deliberately different for scanning and purchasing:

- A scan target must be an eligible active member because recommendation context and scan access are current capabilities.
- A purchase owner may be any member of the current family, including a deactivated member, so users can record gifts and backfill ownership history.
- A caller must still be an authenticated active member of the current family.
- Foreign-family member ids are rejected.

The owner selector defaults to the scan target and lists all members of the current family with their status visible where needed.

## Web Flow

Extend the existing scan review surface rather than creating a separate checkout area.

For each recognized candidate, the UI should show:

- recommendation score and label;
- concise recommendation reasons;
- metadata matches with a selectable preferred edition;
- duplicate warning and edition follow-up hint;
- a `Bought this book` action.

The purchase interaction should collect the owner member first, defaulting to the scan target, then show only the minimum optional intake fields. The action is disabled while saving. On success, the candidate is marked as purchased or removed from the pending review list and the UI shows the owning member and created-library result. On validation or duplicate-related errors, the selected metadata and form values remain intact.

## API and Domain Boundaries

- Keep `ManualBookIntakeRecorder` as the domain/application entry point for creating copies.
- Extract or reuse a canonical metadata import operation so the purchase endpoint does not duplicate provider/provenance rules.
- Add a dedicated purchase request/response contract instead of exposing persistence entities.
- Extend the existing book-copy ownership boundary to accept an explicit owner where the general manual-intake flow needs it; the scan purchase endpoint must always pass its validated owner.
- Keep recommendation scoring separate from duplicate detection and canonical import.

## Failure Handling

- No authenticated active caller: `401 Unauthorized`.
- Scan session/candidate outside the current family or expired: `404 Not Found`.
- Owner outside the current family: `400 Bad Request` or `404 Not Found` according to the existing family-isolation convention; do not reveal foreign membership details.
- Missing edition identity: validation problem with fields to complete.
- Metadata provider/import failure: no copy is created and the user can retry.
- Duplicate detected: return the warning and require explicit user confirmation; do not silently reject the purchase.
- Double-click protection: disable the UI action during submission and keep the operation transactional. A future increment may add an idempotency key if real-device retries show duplicate submissions.

## Validation

### Application/domain tests

- deterministic scoring for matching and non-matching authors/languages;
- neutral behavior when metadata signals are absent;
- stable score bounds and reason generation;
- duplicate detection remains independent from recommendation score.

### API integration tests

- active family member can purchase for another active member;
- active family member can purchase for a deactivated member in the same family;
- non-admin and admin callers follow the same owner rule;
- foreign-family and unauthenticated callers are rejected;
- selected metadata is imported or reused and the copy is created in one flow;
- duplicate warnings are returned without blocking after explicit confirmation;
- incomplete edition metadata returns validation without creating a copy;
- import or intake failure leaves no partial copy.

### Web tests

- recommendation score and reasons render;
- owner selector defaults to the scan target and permits any family member;
- purchase confirmation sends the selected metadata, owner, and intake fields;
- success state identifies the owner and created copy;
- validation and server errors preserve the user's form state.

### Manual verification

Run a real or representative shelf photo through the local mobile flow, select a non-current owner, confirm a duplicate warning, and verify the resulting copy in the family library. Check logs for recognition, metadata import, duplicate detection, and purchase completion.

## Out of Scope

- Wishlist UI or conversion from wishlist to owned books.
- Batch purchase checkout.
- Automatic preference learning.
- AI as the authoritative scoring mechanism.
- Full genre/style/age classification before the base flow is working.
- Redesign of family invitations or authentication.
