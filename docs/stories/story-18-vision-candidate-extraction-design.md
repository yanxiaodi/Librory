# Story 18 Design: MAF-Driven Book Recognition Workflow

## Goal

Improve shelf-photo recognition by using Microsoft Agent Framework to orchestrate a single server-side workflow that receives an uploaded image, extracts structured book candidates with a vision-capable LLM, enriches them through Google Books, and returns structured results to the client.

The goal is to reduce OCR noise, avoid querying metadata for every raw text line, and improve ranking when a shelf photo contains many spines, author names, publisher text, and other clutter.

## Scope

In scope:

- Keep a single web API entry point for scan uploads
- Store the uploaded image on the server and start an async recognition job
- Use Microsoft Agent Framework to orchestrate the recognition workflow inside the API process
- Use a vision-capable LLM to extract structured candidates with title, optional author hint, evidence text, and confidence
- Expose Google Books as a workflow tool for metadata enrichment and disambiguation
- Re-rank Google Books results using title and author agreement
- Return a small, high-quality candidate list to the existing recognition job flow

Out of scope:

- Replacing the single upload entry point with multiple client-side calls
- Changing the scan-session or manual-intake flows
- Building a separate AI service outside the API project
- Adding recommendation reasoning in this story

## Story Slice

This story is a recognition-quality slice inside the existing shelf scan flow.

It does not change the public scan-session boundary. It changes how the recognition job produces candidates before the result is persisted and shown in the UI.

## Recommended Approach

Use a workflow-driven recognition pipeline:

1. The API receives the uploaded image through a single scan endpoint and stores it temporarily.
2. Microsoft Agent Framework orchestrates a server-side workflow inside the API process.
3. The workflow calls a vision-capable Azure OpenAI model to extract structured candidate books from the image.
4. The workflow calls Google Books as a tool to enrich and disambiguate the strongest candidates.
5. The workflow returns structured recognition results to the job processor, which persists them for the client.

The structured candidate should include:

- title
- optional author hint
- evidence text or evidence lines
- confidence

The workflow should then:

- normalize and deduplicate the structured candidates
- query Google Books by title, and by title plus author when an author hint exists
- re-rank metadata matches by title similarity and author agreement
- keep only the strongest final candidates

## UI or API Design

The public API shape can stay the same for the first version if the recognition job still returns ranked candidates and metadata matches.

Internally, the recognition pipeline should move from string-only fallback output to a structured candidate model.

Recommended internal candidate shape:

- `DisplayTitle`
- `AuthorHint`
- `EvidenceText`
- `Rank`
- `MetadataMatches`

If the author is not visible on the spine, the field should remain empty.

## Behavior or Data Flow

1. The user uploads a shelf photo.
2. The API stores the image and creates a recognition job.
3. The server-side workflow receives the image.
4. The LLM returns a short list of structured book candidates.
5. The workflow calls Google Books only for those candidates.
6. The workflow re-ranks metadata results using title and author agreement.
7. The job returns a compact candidate list with metadata matches and warnings.

Suggested ranking rules:

- exact or near-exact title match gets the highest weight
- author agreement boosts the score
- title match without author agreement is still allowed, but ranked lower
- conflicting author evidence lowers confidence
- candidates with no author hint remain valid

## Testing

Add coverage for:

- structured candidate extraction from the vision workflow
- empty author hints when the spine does not show an author
- Google Books lookup by title only and by title plus author
- metadata re-ranking when multiple Google Books results share the same title
- fallback behavior when the vision workflow returns no candidates

## Dependencies

- Azure OpenAI vision-capable chat model
- Microsoft Agent Framework for orchestration inside the API project
- Google Books metadata search exposed as a workflow tool

## Story Boundary

This story owns the recognition workflow, candidate extraction quality, and metadata re-ranking.

It does not own:

- OCR provider migration
- scan-session persistence
- candidate correction
- manual intake
- duplicate detection

Those remain separate stories or existing flows.
