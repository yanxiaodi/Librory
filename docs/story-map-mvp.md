# Story Map: Librory MVP

## 1. Purpose

Librory MVP helps a family make fast buying decisions while standing in front of a second-hand book shelf.

The first release focuses on the shopping journey first:

1. Scan the shelf.
2. Identify books.
3. Rank books by recommendation strength.
4. Warn about duplicates.
5. Let the user correct mistakes quickly.
6. Let the user save purchased books into the family library.

The app supports both single-user and family-account setups. Family membership matters mainly for ownership and duplicate checks.

## 2. Scope Rules

### In scope

- Shelf scanning from a shelf photo.
- Book recognition from shelf photos.
- Recommendation scoring and explanation.
- Duplicate detection against the family library.
- Manual correction of recognition results.
- Re-scan flow for purchased books.
- Fast intake with optional later completion.
- Single-user accounts.
- Family groups with invited members.
- Bilingual UI support for English and Chinese.
- Bilingual book metadata display where available.
- Wishlist support.

### Out of scope for MVP

- Automatic preference learning.
- Author completion dashboards.
- Collection statistics dashboards.
- Public reviews.
- Deep market valuation.
- Complex permission management.

## 3. Personas

### Parent

The parent buys books for the family, scans shelves, checks duplicates, and assigns ownership.

### Family member

A family member can have books assigned to them and can contribute to the family library through the same core flows.

## 4. Story Map

### Epic 1: Scan a Shelf and Decide Fast

Goal: help the user decide what is worth buying while browsing a second-hand shop.

- As a parent, I want to take a photo of a shelf and see a list of detected books.
- As a parent, I want each detected book to show title, author, and recommendation strength.
- As a parent, I want the list to prioritize the most promising books first.
- As a parent, I want low-confidence matches to be visible so I know which results are uncertain.
- As a parent, I want the app to keep working even when some books are not recognized perfectly.

Acceptance criteria:

- The user can submit one shelf photo and receive multiple book candidates.
- Each candidate includes at least a title and a confidence level.
- The list can continue when some items are incomplete.
- The UI clearly marks uncertain matches.

### Epic 2: Correct Recognition Errors

Goal: let the user fix the few mistakes that matter without restarting the whole flow.

- As a parent, I want to re-scan a single book if the shelf scan was wrong.
- As a parent, I want to manually enter a title if the system cannot recognize a book.
- As a parent, I want the corrected result to fetch book metadata again.
- As a parent, I want the corrected result to refresh recommendation and duplicate status.

Acceptance criteria:

- The user can correct an item from the scan result list.
- The user can either re-scan or type a title manually.
- After correction, the system re-runs metadata lookup and scoring.

### Epic 3: Recommend Books

Goal: show whether a book matches the user or the family’s reading preferences.

- As a parent, I want recommendations based on age range.
- As a parent, I want recommendations based on themes, style, and genre.
- As a parent, I want recommendations based on similar books.
- As a parent, I want recommendations based on similar authors.
- As a parent, I want an explanation for why a book was recommended.

Acceptance criteria:

- Recommendation uses both rule-based signals and AI-assisted reasoning.
- The first version does not require automatic learning.
- The user can configure common preference ranges.
- Recommendation and duplicate warnings are separate signals.

### Epic 4: Detect Duplicates

Goal: prevent accidental repeat purchases.

- As a parent, I want the app to check whether the family already owns a book.
- As a parent, I want the app to check across the whole family library, not just my own books.
- As a parent, I want duplicate warnings to appear during shelf scanning.
- As a parent, I want duplicate warnings to still allow purchase if the new copy is a better edition or better condition.
- As a parent, I want version differences to be visible when they are detected.

Acceptance criteria:

- Duplicate detection runs during shelf review.
- Duplicate detection treats already-owned as a warning, not a hard stop.
- The user can continue buying after seeing a warning.
- Edition differences are shown when available.

### Epic 5: Fix or Recheck Before Buying

Goal: support the reality that shelf photos can be messy.

- As a parent, I want to re-run recognition on one book after correcting it.
- As a parent, I want to keep moving through the shelf when most books are fine.
- As a parent, I want corrected items to re-enter the same result list.

Acceptance criteria:

- One bad match does not cancel the whole shelf scan.
- The user can edit only the incorrect items.
- The corrected item stays part of the current shelf session.

### Epic 6: Save Purchased Books

Goal: record books after purchase with low friction.

- As a parent, I want to save a purchased book into the family library.
- As a parent, I want to start with only the minimum fields.
- As a parent, I want optional fields to be filled in later.
- As a parent, I want to assign the book to a family member.
- As a parent, I want to store a cover photo and book details.

Acceptance criteria:

- The user can save a book with minimal required input.
- Optional fields are visible but not required.
- The saved book belongs to the family library.

### Epic 7: Manage a Family Library

Goal: support both single-user and family-account usage.

- As a user, I want to register as a single user.
- As a parent, I want to create a family group.
- As a parent, I want to invite other users to the family group.
- As a parent, I want to assign a book to any family member.
- As a parent, I want duplicate checks to use the whole family library.

Acceptance criteria:

- The app supports one-person and family setups.
- Family members can share the same library scope.
- Ownership is a first-class field on each saved copy.

### Epic 8: Support Bilingual UI and Book Data

Goal: make the app usable in English and Chinese.

- As a user, I want the UI in English or Chinese.
- As a user, I want book search results to show both languages when available.
- As a user, I want metadata fields to preserve both languages when possible.
- As a user, I want the app to prefer my selected language when it exists.

Acceptance criteria:

- UI text comes from language resources.
- The app supports English and Chinese.
- Book display can mix both languages when data exists in both.

### Epic 9: Wishlist

Goal: keep track of books the user wants to buy later.

- As a parent, I want to add a book to a wishlist.
- As a parent, I want to add wishlist items from search results or scanning.
- As a parent, I want the wishlist to stay separate from owned books.

Acceptance criteria:

- The wishlist is persistent.
- The same book can appear in the wishlist before purchase.
- Wishlist items can be converted into owned books later.

## 5. Priority Order

### P0

- Scan shelf photo.
- Identify books.
- Rank books by recommendation.
- Show duplicate warnings.
- Allow manual correction.
- Save purchased books with minimal intake.
- Support family ownership.
- Support English and Chinese UI.

### P1

- Better edition-aware duplicate checks.
- Wishlist.
- Better metadata display in both languages.

### P2

- Author collection progress.
- Statistics dashboard.
- Reviews and reading notes.
- Automatic preference learning.

## 6. First Release Notes

The first release should be good at one thing:

helping a parent stand in front of a second-hand shelf and avoid bad purchases.

Everything else should support that flow, not distract from it.

