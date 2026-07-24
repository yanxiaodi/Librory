# Backend Story Map

This document breaks the Librory backend into numbered stories so implementation can proceed in a stable order.

The stories are derived from:

- `docs/prd.md`
- `docs/mvp-scope.md`
- `docs/data-model.md`
- `docs/architecture.md`
- `docs/implementation-plan.md`

The backend should be implemented as an ASP.NET Core API, with AI orchestration kept inside the API project rather than split into a separate service.

## Story Order

| ID | Story | Purpose | MVP Phase | Depends On |
| --- | --- | --- | --- | --- |
| story-01 | Identity, family, and login | Let a user sign in, establish a profile, and join or create a family scope | Phase 1 | None |
| story-02 | Core book domain model | Persist Work, Edition, Copy, and supporting book metadata | Phase 1 | story-01 |
| story-03 | Manual book intake | Let a user save a purchased book with minimal required data | Phase 1 | story-01, story-02 |
| story-04 | Shelf scan sessions | Create temporary scan sessions and store scan results | Phase 2 | story-01, story-02 |
| story-05 | Duplicate detection | Check the family library for existing copies during scan and intake | Phase 2 | story-01, story-02, story-03 |
| story-06 | Recommendation profiles | Persist manual preferences and produce recommendation inputs | Phase 2 | story-01, story-02 |
| story-07 | Wishlist | Save books the user wants to buy later | Phase 2 | story-01, story-02 |
| story-08 | Localization-aware shaping | Return bilingual and preferred-language book data | Phase 1 | story-01, story-02 |
| story-09 | AI orchestration | Run recognition, enrichment, recommendation, and duplicate workflows inside the API | Phase 3 | story-04, story-05, story-06 |

## Story-01: Identity, Family, and Login

### Goal

Allow a user to authenticate, create or join a family scope, and establish the member record that the rest of the product uses.

### Why this comes first

Every downstream capability depends on knowing:

- who the user is
- which family scope they belong to
- whether they are acting as an individual user or a family member
- which member owns a copy or wishlist item

The first release should keep authentication simple and support the login providers already used in Koviva:

- Google login
- Microsoft login

### Sub-Stories

#### story-01a: External Identity Resolution

Resolve Google or Microsoft sign-in into a stable external identity record.

Acceptance criteria:

- The backend can identify a user by provider plus provider subject.
- The backend can store more than one external provider for the same account.
- The backend does not treat email as the primary identity key.
- The backend can reuse the same account when the user signs in with a previously linked provider.

#### story-01b: Family and Member Persistence

Persist the domain objects that represent a family, its members, and the member role.

Acceptance criteria:

- The backend can create and persist a `Family`.
- The backend can create and persist a `Member`.
- A member belongs to exactly one family.
- A member has a role of at least `admin` or `member`.
- A member has a `preferredLanguage` value.

#### story-01c: First-Login Family Bootstrap

Create the default family and member records when a user signs in for the first time.

Acceptance criteria:

- The backend can create a singleton family for an individual user.
- The backend can create an initial admin member when no family exists yet.
- The bootstrap flow is atomic enough that partial records are not left behind when creation fails.
- The resulting identity is ready for downstream family-scoped requests.

#### story-01d: Shared Family Creation

Create a family group that can later accept invited members.

Acceptance criteria:

- The backend can create a family group distinct from a singleton family.
- The initial creator becomes the family admin.
- The family has a stable identifier that later endpoints can reference.
- The model leaves room for invitation and membership management in later stories.

#### story-01e: Current Family Context

Resolve the authenticated user to the current family and member for every family-scoped request.

Acceptance criteria:

- The backend can derive the active family from the signed-in identity.
- The backend can derive the current member from the active family.
- Family-scoped business operations can require that context without re-deriving it in each handler.
- Authorization failures are returned when the identity does not map to a valid family member.

### Scope

In scope:

- Sign-in through Google
- Sign-in through Microsoft
- Creation of a user profile on first login
- Creation of a family record for a new individual user or family group
- Member records with at least `admin` and `member` roles
- A singleton family mode for individual users
- Family membership lookup for all later API requests
- A `preferredLanguage` field on the user or member profile

Out of scope:

- Complex permission hierarchies
- Platform-admin tooling
- Multi-tenant enterprise identity
- Invitation email templates and delivery infrastructure if the product can defer them to a later story

### User Stories

- As a user, I want to sign in with Google or Microsoft so I can access the app without a custom password system.
- As an individual user, I want the system to create a personal family scope so I can use the app without inviting anyone.
- As a family admin, I want to create a family group so other members can share the same library.
- As a family member, I want my role to be tracked so the app knows who can manage family settings.
- As a user, I want a preferred language on my profile so the UI can choose English or Chinese.

### Acceptance Criteria

- The API supports authentication through Google and Microsoft login providers.
- A successful first login creates a persisted user profile if one does not already exist.
- The system can create a singleton family for an individual user.
- The system can create a family group for shared use.
- The API stores the authenticated member's role within the family scope.
- The API can resolve the current user to a family and member record for downstream requests.
- The API stores a preferred language value for the user or member profile.
- The story does not require a complex permission engine beyond the role field.

### Notes

- This story should align with the existing Koviva login pattern rather than inventing a new auth flow.
- If the implementation uses Azure-backed identity, the story should still expose the same Google and Microsoft sign-in outcome at the API boundary.
- Family invitation and admin transfer can be introduced later as separate stories if they are not required for the first usable login path.
- The sub-stories above are intentionally ordered so identity comes before family bootstrap, and family bootstrap comes before request-scoped resolution.

## Story-02: Core Book Domain Model

### Goal

Persist the core book entities so the app can distinguish between a work, an edition, and a physical copy.

### Sub-Stories

#### story-02a: Work and Edition Identity

Create the core relationship between a book work and one or more editions.

Acceptance criteria:

- The backend can create a `BookWork`.
- The backend can create a `BookEdition`.
- A work can own many editions.
- An edition must belong to exactly one work.
- The model can distinguish a work from one of its editions.

#### story-02b: Copy Ownership and Family Inventory

Attach a physical copy to a family and one owning member.

Acceptance criteria:

- The backend can create a `BookCopy`.
- A copy must belong to exactly one edition.
- A copy must belong to exactly one family.
- A copy must belong to exactly one owning member.
- The model can represent the family library as owned physical copies.

### Modeling Note

`BookCopy` is the family-owned entity. It represents the physical book that was purchased, stored, and assigned to a member. A copy should not exist without a work-through-edition chain, because ownership and duplicate detection both depend on knowing which edition was actually acquired.

#### story-02c: Book Metadata and Inference Fields

Store the metadata that later search, intake, and recommendation flows will need.

Acceptance criteria:

- The model can store author and title data on the work or edition where appropriate.
- The model can store source metadata and confidence values for inferred data.
- The model can carry book data that may later be localized or enriched.
- The metadata model does not force a persistence schema yet.

### User Stories

- As the system, I want to store a work so that multiple editions can point to the same title.
- As the system, I want to store an edition so that hardcover, paperback, and special editions remain distinct.
- As the system, I want to store a copy so that a family-owned physical book can be tracked independently of the edition.
- As the system, I want to store source metadata and confidence values where book data is inferred.

### Modeling Note

The `Work -> Edition -> Copy` split is not just for storage. It also defines the unit of analysis for future features:

- `BookWork` for cross-family popularity, search, and recommendation aggregation
- `BookEdition` for version-specific comparison, popularity, and collector value
- `BookCopy` for family-owned inventory, ownership, and purchase records

Any future ranking, report, or dashboard must declare which level it counts at before implementation starts.

### Acceptance Criteria

- The database supports `Work`, `Edition`, and `Copy` as separate entities.
- A work can have many editions.
- An edition can have many copies.
- A copy belongs to one family and one owning member.
- The model can store uncertain or inferred metadata without overwriting source facts.

## Story-03: Manual Book Intake

### Goal

Let a user save a purchased book into the family library with the minimum viable intake flow.

### User Stories

- As a family admin or member, I want to save a purchased book so it becomes part of the family library.
- As a family admin or member, I want to start with only the minimum required fields so intake stays fast.
- As a family admin or member, I want to complete optional fields later so I do not lose momentum at the shelf or at home.

### Modeling Note

`story-03` only creates `BookCopy` records. It assumes the book has already been resolved to a `BookEdition` by a separate catalog or recognition flow. If the edition is not known yet, the user must resolve that first rather than creating a provisional copy.

### Acceptance Criteria

- The API can create a persisted copy for a purchased book.
- The minimum intake path records edition, owning member, and duplicate confirmation state.
- Optional fields such as condition, price, store, shelf location, purchase date, and notes can be added later.
- The saved record belongs to the current family scope.

## Story-04: Shelf Scan Sessions

### Goal

Create temporary shelf scan sessions so users can review results without immediately committing them to the library.

### User Stories

- As a family member, I want to start a scan session from a shelf photo so I can review books before buying.
- As a family member, I want scan results to persist temporarily so I can return to them soon after scanning.
- As a family member, I want low-confidence or incomplete results to remain visible so uncertain matches are not hidden.

### Acceptance Criteria

- The API can create a scan session.
- The API can persist multiple recognized candidates under one session.
- The API can retain a session for a short, configurable retention window.
- The API can continue to return partial results when not every item is recognized perfectly.

## Story-05: Duplicate Detection

### Goal

Check the family library for already-owned books and surface warnings during scan and intake.

### User Stories

- As a family member, I want duplicate checks to run against the whole family library.
- As a family member, I want duplicate warnings to appear during shelf scanning so I can decide quickly.
- As a family member, I want a better edition or better condition to still be visible as a potential reason to buy again.

### Acceptance Criteria

- Duplicate detection can search across the family scope, not just the current member's books.
- Duplicate detection returns a warning rather than a hard block.
- Duplicate output can include edition differences when the system has enough information.
- The same duplicate logic can be reused during shelf review and home intake.

## Story-06: Recommendation Profiles

### Goal

Store manual reading preferences and return recommendation inputs that the UI and AI workflow can use.

### User Stories

- As a family member, I want to record age range and preference settings so recommendations can be personalized.
- As a family member, I want recommendation results to include an explanation so I understand the score.

### Acceptance Criteria

- The API persists a `RecommendationProfile` or equivalent model.
- The model supports at least age range, favorite authors, favorite genres, and favorite styles.
- The API can produce recommendation inputs that combine rules with AI-assisted reasoning later.
- Recommendation output remains separate from duplicate warnings.

## Story-07: Wishlist

### Goal

Let users save books they want to buy later without marking them as owned.

### User Stories

- As a user, I want to add a book to a wishlist so I can find it later.
- As a user, I want to add wishlist items from search or scan results so capture is fast.

### Acceptance Criteria

- The API persists wishlist items separately from owned copies.
- A wishlist item can reference a work, edition, or fuzzy match result.
- A wishlist item can later be converted into an owned copy.

## Story-08: Localization-Aware Shaping

### Goal

Return book data in a language-aware shape so the frontend can prefer English or Chinese without duplicating business logic.

### User Stories

- As a user, I want the system to return localized book fields so the UI can show the best available language.
- As a user, I want the API to preserve both languages where available so data is not lost.

### Acceptance Criteria

- The API can return localized variants for title, subtitle, summary, genre labels, recommendation text, and duplicate warning text.
- The API can prefer the user's selected language and fall back to the alternate language when needed.
- The API keeps English as the canonical source where no localized value exists.

## Story-09: AI Orchestration

### Goal

Run recognition, enrichment, recommendation, and duplicate workflows inside the API project using modular workflow code.

### User Stories

- As the system, I want a scan workflow so I can turn a shelf photo into candidate books.
- As the system, I want an enrichment workflow so I can add metadata to candidates.
- As the system, I want a recommendation workflow so I can score books with explainable output.
- As the system, I want a duplicate workflow so I can surface warnings alongside recommendations.

### Acceptance Criteria

- The AI workflow lives inside the main API project.
- The workflow is isolated from controllers and persistence code.
- The workflow returns structured output that the API can persist.
- The workflow does not own the database schema or UI state.

## Priority Summary

### P0

- story-01 Identity, family, and login
- story-02 Core book domain model
- story-03 Manual book intake
- story-08 Localization-aware shaping

### P1

- story-04 Shelf scan sessions
- story-05 Duplicate detection
- story-06 Recommendation profiles
- story-07 Wishlist

### P2

- story-09 AI orchestration

