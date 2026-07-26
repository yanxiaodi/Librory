# Story 03b Manual Intake API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose the existing manual intake flow as a family-scoped API that creates a `BookCopy` from a resolved `BookEdition`, keeps duplicate detection warning-only, and returns the created copy plus the duplicate summary.

**Architecture:** Keep this slice thin. Add a small `book-copies` endpoint group in the API project, reuse the existing `ManualBookIntakeRecorder` helper, and leave external metadata providers and front-end work for later stories.

**Tech Stack:** C# / .NET 10, xUnit

---

### Task 1: Add manual intake API contracts

**Files:**
- Create: `src/Librory.Api/Contracts/CreateBookCopyRequest.cs`
- Create: `src/Librory.Api/Contracts/BookCopyResponse.cs`
- Create: `src/Librory.Api/Contracts/ManualBookIntakeResponse.cs`
- Create: `src/Librory.Api/Contracts/BookCopyResponseFactory.cs`
- Create: `src/Librory.Api/Contracts/ManualBookIntakeResponseFactory.cs`

- [ ] Define the request payload for resolved edition intake and optional purchase metadata.
- [ ] Define the response payload for a created copy and duplicate summary.
- [ ] Add factories that map domain/application objects into API contracts.

### Task 2: Add manual intake endpoints

**Files:**
- Create: `src/Librory.Api/Endpoints/BookCopyEndpoints.cs`
- Update: `src/Librory.Api/Program.cs`

- [ ] Add a family-scoped `POST /api/family/current/book-copies` endpoint.
- [ ] Add a family-scoped `GET /api/family/current/book-copies/{bookCopyId}` endpoint.
- [ ] Reuse the current family/member context and `ManualBookIntakeRecorder`.

### Task 3: Add API integration coverage

**Files:**
- Update: `tests/Librory.Api.Tests/ApiIntegrationTests.cs`

- [ ] Create a copy from a resolved edition and verify the response.
- [ ] Read the created copy back from the fetch endpoint.
- [ ] Verify duplicate detection is surfaced when the family already owns the same edition/work.
- [ ] Verify missing editions return `404 Not Found`.

### Task 4: Keep docs aligned

**Files:**
- Update: `docs/backend-story-map.md`
- Update: `docs/api-reference.md`
- Add: `docs/devlog/2026-07-26-story-03b-manual-intake-api.md`

- [ ] Add `story-03b` to the backend story map.
- [ ] Document the new API routes and response shape.
- [ ] Record the implementation decision and validation in a devlog entry.
