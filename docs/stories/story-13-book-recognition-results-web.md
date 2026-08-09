# Story 13 Design: Book Recognition Results Web

## Goal

Build the first user-facing web slice for the book recognition flow: after a user uploads a shelf or cover photo, the app should poll the recognition job and show a reviewable list of candidate books with metadata.

## Scope

In scope:

- A phone-friendly upload and review flow in the web app
- Polling the recognition job until results are ready
- Rendering Document Intelligence-derived candidates with metadata matches
- Showing confidence, warnings, and evidence for each candidate
- Letting the user remove obvious false positives
- Letting the user edit the search text for a candidate
- Keeping the flow separate from manual intake and recommendation scoring

Out of scope:

- Manual book intake into the family library
- Duplicate detection logic beyond displaying existing warning data
- Recommendation scoring changes
- Canonical catalog import
- Long-lived scan history

## Story Slice

This story is the first review step after photo upload.

The user flow is:

1. Open the scan screen.
2. Upload a shelf photo or a single book-cover photo.
3. The API creates a recognition job and returns a job id immediately.
4. The frontend polls the job until the result is ready.
5. The app shows a list of likely book titles and their matched metadata.
6. The user removes obvious false positives or edits the search text before moving to the next step.

The backend flow already exists or is being built in the recognition job slice. This story focuses on the web experience that consumes that job result.

## Recommended Approach

Use the existing scan and recognition context as the source of truth, and add a dedicated review screen that is optimized for quick triage on mobile.

Why this is the right next story:

- The recognition backend already has a clear job boundary.
- The current app still lacks a focused review UI for the returned candidates.
- The review step is the natural bridge between recognition and later correction or intake flows.
- Keeping this slice narrow avoids mixing upload, recognition, correction, and intake into one large change.

Rejected alternatives:

- Folding this into manual intake would blur two different user decisions.
- Jumping straight to correction would leave the user without a usable review screen.
- Expanding the scan page into intake would make the first recognition slice too large.

## UI Design

The review screen should feel like a fast decision surface, not a data-heavy results page.

Suggested states:

- Idle: show the capture or upload action
- Uploading: show the file upload is in progress
- Processing: show a scanning state with polling
- Ready: show the recognized candidate list
- Error: show retry affordance

The ready state should show:

- candidate title
- author or metadata match when available
- confidence or match strength
- duplicate or uncertainty warning when available
- a short evidence summary from Document Intelligence or recognition context

The user should be able to:

- remove a false positive from the list
- edit the search text for a candidate
- retry the recognition job if the result is poor

## Data Flow

1. User uploads a shelf or cover photo.
2. API creates a recognition job.
3. Frontend polls the job status.
4. When the job completes, the frontend renders the candidate list.
5. The user trims or edits the list before any later correction or intake workflow.

## Testing

Add coverage for:

- rendering the upload state
- polling a recognition job until completion
- showing the ready-state candidate list
- removing a candidate from the review list
- editing candidate search text
- handling a failed recognition job with retry affordance

## Dependencies

This story depends on:

- authenticated web sessions
- current family context
- the recognition job API
- the existing temporary image storage and Document Intelligence pipeline

It does not depend on manual intake or recommendation scoring.

## Story Boundary

This story is complete when:

- a user can upload a shelf or cover photo from the web app,
- the app polls the recognition job,
- the app shows a reviewable candidate list with metadata,
- and the user can trim or edit the candidate set before moving on.
