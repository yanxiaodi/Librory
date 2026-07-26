# Story 07b Wishlist API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose wishlist paging and item creation as a family-scoped API so the frontend can list, create, and fetch wishlist items without duplicating domain rules.

**Architecture:** Keep the slice thin. Add a wishlist endpoint group in the API project, reuse the existing wishlist domain/application flow, and keep conversion to owned copies for later stories.

**Tech Stack:** C# / .NET 10, xUnit

---

### Task 1: Add wishlist API contracts

**Files:**
- Create: `src/Librory.Api/Contracts/WishlistPageResponse.cs`
- Create: `src/Librory.Api/Contracts/WishlistItemDto.cs`
- Create: `src/Librory.Api/Contracts/CreateWishlistItemRequest.cs`
- Create: `src/Librory.Api/Contracts/WishlistItemDtoFactory.cs`

- [x] Define the list response payload for paged wishlist items.
- [x] Define the create request payload for a wishlist item.
- [x] Define the response payload for a single wishlist item.

### Task 2: Add wishlist endpoints

**Files:**
- Create: `src/Librory.Api/Endpoints/WishlistEndpoints.cs`
- Update: `src/Librory.Api/Program.cs`

- [x] Add a family-scoped `GET /api/family/current/wishlist` endpoint.
- [x] Add a family-scoped `POST /api/family/current/wishlist` endpoint.
- [x] Add a family-scoped `GET /api/family/current/wishlist/{wishlistItemId}` endpoint.

### Task 3: Add API integration coverage

**Files:**
- Update: `tests/Librory.Api.Tests/ApiIntegrationTests.cs`

- [x] Page the current family's wishlist.
- [x] Create a wishlist item.
- [x] Fetch a wishlist item back by id.

### Task 4: Keep docs aligned

**Files:**
- Update: `docs/backend-story-map.md`
- Update: `docs/api-reference.md`

- [x] Add `story-07b` to the backend story map.
- [x] Document the wishlist routes and paging behavior.
