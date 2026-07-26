# Story 06b Recommendation Profile API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose `RecommendationProfile` as a family-scoped API for the current member, persist it in PostgreSQL, and keep partial updates and validation aligned with the existing domain model.

**Architecture:** Keep the slice narrow. Add a recommendation-profile endpoint pair in the API project, persist the domain entity with EF Core, and reuse the current member auth context. Do not add recommendation ranking or AI logic here.

**Tech Stack:** C# / .NET 10, xUnit

---

### Task 1: Persist recommendation profiles

**Files:**
- Create: `src/Librory.Infrastructure/Persistence/Configurations/RecommendationProfileConfiguration.cs`
- Update: `src/Librory.Infrastructure/Persistence/LibroryDbContext.cs`
- Update: `src/Librory.Infrastructure/Persistence/Configurations/FamilyConfiguration.cs` if needed

- [x] Add an EF configuration for `RecommendationProfile`.
- [x] Persist the per-member unique profile and its favorite lists.
- [x] Keep the domain validation and list normalization behavior intact.

### Task 2: Add API contracts and endpoints

**Files:**
- Create: `src/Librory.Api/Contracts/UpsertRecommendationProfileRequest.cs`
- Create: `src/Librory.Api/Contracts/RecommendationProfileResponse.cs`
- Create: `src/Librory.Api/Contracts/RecommendationProfileResponseFactory.cs`
- Create: `src/Librory.Api/Endpoints/RecommendationProfileEndpoints.cs`
- Update: `src/Librory.Api/Program.cs`

- [x] Add `GET /api/family/current/recommendation-profile`.
- [x] Add `PUT /api/family/current/recommendation-profile`.
- [x] Return the current member's profile and preserve existing values on partial updates.

### Task 3: Add API integration coverage

**Files:**
- Update: `tests/Librory.Api.Tests/ApiIntegrationTests.cs`

- [x] Create and fetch a recommendation profile for the current member.
- [x] Verify partial updates preserve existing preferences.
- [x] Verify invalid age ranges fail.

### Task 4: Keep docs aligned

**Files:**
- Update: `docs/backend-story-map.md`
- Update: `docs/api-reference.md`
- Add: `docs/devlog/2026-07-26-story-06b-recommendation-profile-api.md`

- [x] Add the API slice to the backend story map.
- [x] Document the new recommendation profile routes.
- [x] Record the implementation and validation in a devlog note.
