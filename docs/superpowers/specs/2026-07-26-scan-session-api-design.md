# Story 04d Design: Scan Session API

## Goal

Expose the existing temporary scan session workflow as a family-scoped API so the frontend can create, review, correct, resolve, and discard scan data without duplicating domain rules.

## Scope

In scope:

- `POST /api/family/current/scan-sessions`
- `GET /api/family/current/scan-sessions/{scanSessionId}`
- `PUT /api/family/current/scan-sessions/{scanSessionId}/candidates/{candidateId}`
- `POST /api/family/current/scan-sessions/{scanSessionId}/candidates/{candidateId}/resolve`
- `DELETE /api/family/current/scan-sessions/{scanSessionId}/candidates/{candidateId}`
- Request and response contracts for scan sessions and candidates
- API integration tests and API reference updates

Out of scope:

- Front-end pages or routing
- OCR or image recognition logic
- Long-running AI orchestration
- External metadata provider lookup

## Design

Treat the scan session as a temporary family-scoped resource.

### Create flow

The `POST` endpoint accepts a shelf photo path, optional recognized candidates, and an optional retention window. It stores the session temporarily for later review and returns the created session payload.

### Read flow

The `GET` endpoint returns the stored session and its candidates when the session exists for the current family. Expired or cross-family sessions are treated as not found.

### Correction flow

The candidate correction endpoint updates a single candidate in place without resetting the rest of the session. Downstream duplicate and recommendation refresh logic stays outside the API slice.

### Resolution flow

The candidate resolution endpoint promotes a candidate into canonical book catalog data when the system has enough evidence or the user confirms it.

### Discard flow

The candidate discard endpoint removes the candidate from the temporary session without promoting it.

## Behavior

- Missing auth returns `401 Unauthorized`
- Missing or unknown sessions return `404 Not Found`
- Invalid candidate or retention data returns `400 Bad Request`
- The current family context scopes all reads and writes

## Testing

Add API integration coverage for:

- creating a scan session
- reading a scan session back
- correcting a single candidate
- resolving a candidate into canonical catalog data
- discarding a candidate
- rejecting cross-family access with `404 Not Found`

## Risks

- The scan session shape is temporary by design, so frontend work should avoid depending on fields that are likely to change.
- The API should not take on recognition or OCR responsibilities; those belong in later workflow slices.
