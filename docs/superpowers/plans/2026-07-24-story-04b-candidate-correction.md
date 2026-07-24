# Story 04b Candidate Correction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the smallest useful domain-level correction flow for `story-04b`: let a scan session update one candidate in place while leaving the rest of the session untouched.

**Architecture:** Keep this slice domain-only. Add explicit correction behavior to `ScanCandidate` and a session lookup/update method on `ScanSession`. Do not wire application refresh logic or API routes in this slice.

**Tech Stack:** C# / .NET 10, xUnit

---

### Task 1: Add correction behavior to `ScanCandidate`

**Files:**
- Modify: `src/Librory.Domain/Models/ScanCandidate.cs`
- Test: `tests/Librory.Domain.Tests/ScanCandidateTests.cs`

- [x] Add a domain method that updates a candidate in place using corrected display title, author, confidence label, and duplicate/recommendation fields.
- [x] Keep the same trimming and validation rules that `Create` already applies.
- [x] Preserve candidate identity when correction data changes.

### Task 2: Add candidate lookup and correction to `ScanSession`

**Files:**
- Modify: `src/Librory.Domain/Models/ScanSession.cs`
- Test: `tests/Librory.Domain.Tests/ScanSessionTests.cs`

- [x] Add a method that finds a candidate by id and throws when the id does not exist.
- [x] Add a method that applies a correction to the matched candidate without affecting the other candidates.
- [x] Keep the session list and expiration behavior unchanged.

### Task 3: Keep story-04b boundaries explicit

**Files:**
- Update: `docs/backend-story-map.md`

- [x] State that the domain handles in-place candidate correction.
- [x] State that duplicate and recommendation refresh happens later in the application layer.
