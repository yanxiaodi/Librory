# Story 04d Scan Session API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose the temporary scan session workflow as a family-scoped API so the frontend can create, review, correct, resolve, and discard scan data without duplicating domain rules.

**Architecture:** Keep the slice thin. Add a scan-session endpoint group in the API project, reuse the existing session/application workflow, and keep recognition and OCR outside this slice.

**Tech Stack:** C# / .NET 10, xUnit

---

### Task 1: Add scan session API contracts

**Files:**
- Create: `src/Librory.Api/Contracts/ScanSessionRequest.cs`
- Create: `src/Librory.Api/Contracts/ScanSessionResponse.cs`
- Create: `src/Librory.Api/Contracts/ScanCandidateRequest.cs`
- Create: `src/Librory.Api/Contracts/ScanSessionResponseFactory.cs`

- [x] Define the request payload for creating a temporary session.
- [x] Define the request payload for correcting or resolving a candidate.
- [x] Define the response payload for the session and its candidates.

### Task 2: Add scan session endpoints

**Files:**
- Create: `src/Librory.Api/Endpoints/ScanSessionEndpoints.cs`
- Update: `src/Librory.Api/Program.cs`

- [x] Add a family-scoped `POST /api/family/current/scan-sessions` endpoint.
- [x] Add a family-scoped `GET /api/family/current/scan-sessions/{scanSessionId}` endpoint.
- [x] Add correction, resolve, and discard endpoints for individual candidates.

### Task 3: Add API integration coverage

**Files:**
- Update: `tests/Librory.Api.Tests/ApiIntegrationTests.cs`

- [x] Create and fetch a scan session.
- [x] Correct a single candidate without resetting the rest of the session.
- [x] Resolve a candidate into canonical catalog data.
- [x] Discard a candidate from the session.

### Task 4: Keep docs aligned

**Files:**
- Update: `docs/backend-story-map.md`
- Update: `docs/api-reference.md`

- [x] Add `story-04d` to the backend story map.
- [x] Document the scan session routes and response shape.
