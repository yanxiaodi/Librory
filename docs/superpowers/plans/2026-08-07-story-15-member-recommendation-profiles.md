# Story 15 Member Recommendation Profiles Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Extend the existing recommendation profile foundation into a family-membership-scoped, permission-aware profile API.

**Architecture:** Keep `RecommendationProfile` owned by `Member`, with one unique profile per membership. Preserve the existing current-member GET/PUT routes as convenience aliases, and add member-scoped routes that authorize profile owners, administrators, and allowed family readers separately. Use explicit JSON field-presence semantics so omitted fields preserve values while JSON `null` clears nullable or collection fields.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, EF Core/Npgsql, PostgreSQL JSON-friendly list columns, xUnit integration/domain tests.

## Global Constraints

- A profile belongs to a family membership, never directly to a login account or whole family.
- Age ranges and preferred book languages are recommendation signals, not hard filters.
- UI language remains `Member.PreferredLanguage` and is independent from preferred book languages.
- No weights, automatic learning, standardized reading levels, cross-family synchronization, or AI scoring are included.
- Private preference notes are visible only to the profile owner and family administrators.
- Inactive or foreign members cannot be read or edited through the member-scoped profile API.

---

### Task 1: Expand the domain profile model and contracts

**Files:**
- Modify: `src/Librory.Domain/Models/RecommendationProfile.cs`
- Modify: `src/Librory.Domain/Models/RecommendationCategoryCatalog.cs`
- Test: `tests/Librory.Domain.Tests/RecommendationProfileTests.cs`

**Interfaces:**
- Add `ExcludedAuthors`, `ExcludedGenres`, `ExcludedStyles`, `PreferredBookLanguages`, `PreferenceNotes`, `ProfileVisibility`, and `UseInFamilyRecommendations` to `RecommendationProfile`.
- Add a domain update operation that accepts explicit field-presence information and validates age range and note length.
- Keep `Family.GetOrCreateRecommendationProfile` as the single profile-per-member aggregate entry point.

- [x] **Step 1: Write failing domain tests** for exclusion lists, preferred book languages, visibility/use flags, note length validation, explicit clearing, and normalization/deduplication.
- [x] **Step 2: Run** `dotnet test tests/Librory.Domain.Tests/Librory.Domain.Tests.csproj --filter RecommendationProfile` and confirm the new tests fail because the fields and update semantics do not exist.
- [x] **Step 3: Implement** the new fields, `ProfileVisibility` enum, bounded notes, and explicit update semantics without changing age-range ranking meaning.
- [x] **Step 4: Run** the filtered domain tests and confirm they pass.

### Task 2: Update persistence and response projection

**Files:**
- Modify: `src/Librory.Infrastructure/Persistence/Configurations/RecommendationProfileConfiguration.cs`
- Modify: `src/Librory.Application/Recommendations/RecommendationProfileDto.cs`
- Modify: `src/Librory.Application/Recommendations/RecommendationProfileDtoFactory.cs`
- Modify: `src/Librory.Api/Contracts/RecommendationProfileResponse.cs`
- Modify: `src/Librory.Api/Contracts/RecommendationProfileResponseFactory.cs`
- Generate: EF migration `ExpandRecommendationProfiles` under `src/Librory.Infrastructure/Persistence/Migrations/`.
- Modify: `src/Librory.Infrastructure/Persistence/Migrations/LibroryDbContextModelSnapshot.cs`
- Test: `tests/Librory.Application.Tests/RecommendationProfileDtoTests.cs`
- Test: `tests/Librory.Api.Tests/LibroryDbContextModelTests.cs`

**Interfaces:**
- Persist all new profile fields using the existing JSON list conversion pattern and a unique `MemberId` index.
- Return the complete profile shape, including visibility and recommendation-use state.

- [x] **Step 1: Add failing DTO and model metadata assertions** for every new response field and the unique membership index.
- [x] **Step 2: Run** the focused Application/API tests and confirm failure.
- [x] **Step 3: Add EF configuration, migration, DTO fields, and response projection.** The migration must alter only `recommendation_profiles`; it must not modify family membership or account tables.
- [x] **Step 4: Run** the focused tests and confirm persistence metadata and projection pass.

### Task 3: Add member-scoped profile API with permission rules

**Files:**
- Modify: `src/Librory.Api/Contracts/UpsertRecommendationProfileRequest.cs`
- Modify: `src/Librory.Api/Endpoints/RecommendationProfileEndpoints.cs`
- Test: `tests/Librory.Api.Tests/ApiIntegrationTests.cs`

**Interfaces:**
- Add:
  - `GET /api/family/current/members/{memberId}/recommendation-profile`
  - `PUT /api/family/current/members/{memberId}/recommendation-profile`
- Keep `/api/family/current/recommendation-profile` as an alias for the current member.
- Profile owner and active family administrators may create, update, and clear profiles.
- Other active family members may read structured fields only when visibility is `Family`; private notes must be omitted.
- A profile may be used by another member only when visibility is `Family` and `UseInFamilyRecommendations` is `true`; expose this state for later Story 16 target selection.
- Use `JsonElement` field presence: `Undefined` means preserve, `Null` means clear, and a typed value replaces the field. Typed test constructors may treat null arguments as omitted for backward-compatible existing tests.

- [x] **Step 1: Add failing API tests** covering owner create/update, admin update, normal-member denial of editing another member, family-visible read without notes, private read denial/limited projection, inactive/foreign target rejection, explicit clearing, and current-member alias behavior.
- [x] **Step 2: Run** the focused API tests and confirm the new cases fail.
- [x] **Step 3: Implement** route mapping, active-family membership lookup, permission checks based on database role/state, profile loading, partial update handling, and safe response shaping.
- [x] **Step 4: Run** the focused API tests and confirm all permission and update cases pass.

### Task 4: Expose profile availability in family member selection data

**Files:**
- Modify: `src/Librory.Api/Contracts/FamilyMembershipContracts.cs`
- Modify: `src/Librory.Api/Endpoints/FamilyEndpoints.cs`
- Test: `tests/Librory.Api.Tests/ApiIntegrationTests.cs`

**Interfaces:**
- Extend family member list data with `HasRecommendationProfile`, `ProfileVisibility`, and `CanUseForFamilyRecommendations`.
- Do not include private `PreferenceNotes` or other full profile data in the member list.
- Compute usability from active membership, visibility, and `UseInFamilyRecommendations`.

- [x] **Step 1: Add a failing member-list integration test** that creates a private/disabled profile and verifies the list exposes availability without leaking notes.
- [x] **Step 2: Run** the focused API test and confirm failure.
- [x] **Step 3: Implement** a profile-aware member projection scoped to the current family.
- [x] **Step 4: Run** the focused API tests and confirm the target-selection shape is stable.

### Task 5: Document, verify, and prepare the branch

**Files:**
- Create: `docs/devlog/2026-08-07-story-15-member-recommendation-profiles.md`
- Modify: `docs/backend-story-map.md` only if the delivered API contract differs from the existing Story 15 acceptance criteria.

- [x] **Step 1: Record** the permission model, explicit clearing behavior, and the boundary with Story 16/09 in the devlog.
- [x] **Step 2: Run** all Domain, Application, and API tests.
- [x] **Step 3: Run** `dotnet build` for the solution and `git diff --check`.
- [x] **Step 4: Review** the complete diff for family isolation, private-note leakage, stale role claims, and accidental AI/scanning scope.
- [x] **Step 5: Verify** `git status --short` contains only Story 15 files before committing and opening a PR.
