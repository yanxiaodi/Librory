# Story 03 Manual Book Intake Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the smallest useful manual intake flow for `story-03`: create a `BookCopy` for a resolved `BookEdition` inside the current family scope, with duplicate status and optional purchase metadata.

**Architecture:** Keep the first slice application-focused. Reuse the existing `Family.AddBookCopy` and `BookCopy.Create` domain behavior, and add a thin application helper plus tests that prove the intake path preserves the current family/member context. Do not introduce repositories or persistence abstractions in this slice.

**Tech Stack:** C# / .NET 10, xUnit

---

### Task 1: Add the manual intake request model

**Files:**
- Create: `src/Librory.Application/Intake/ManualBookIntakeRequest.cs`
- Test: `tests/Librory.Application.Tests/ManualBookIntakeRequestTests.cs`

- [ ] Define a request record for the intake flow that carries the resolved edition, owning member, and optional purchase details.
- [ ] Verify the request trims or preserves values consistently with the domain model expectations.

### Task 2: Add an application helper for manual intake

**Files:**
- Create: `src/Librory.Application/Intake/ManualBookIntakeRecorder.cs`
- Test: `tests/Librory.Application.Tests/ManualBookIntakeRecorderTests.cs`

- [ ] Add a small helper that takes the current `Family`, current `Member`, and resolved `BookEdition`, then calls `Family.AddBookCopy(...)`.
- [ ] Ensure the helper forwards duplicate status and optional intake metadata without mutating unrelated domain state.
- [ ] Ensure the helper rejects mismatched family/member combinations via the existing domain validation.

### Task 3: Keep docs aligned with the intake boundary

**Files:**
- Update: `docs/backend-story-map.md`
- Update: `docs/prd.md`
- Update: `docs/implementation-plan.md`

- [ ] Keep `story-03` scoped to copy creation rather than work/edition resolution.
- [ ] Keep the PRD minimum intake requirements aligned with the resolved-edition model.
- [ ] Keep the implementation plan explicit that intake is family-scoped and edition-resolved.

