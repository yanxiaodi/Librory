# Story 13 Design: Book Recognition Job

## Goal

Let a signed-in user upload a shelf photo or book-cover photo and receive a set of book-title candidates asynchronously. The system should treat the input image as an unstructured photo, extract likely book titles from it, and then reuse the existing book-metadata search API to enrich the candidates.

## Scope

In scope:

- Async recognition job creation from an uploaded image
- Temporary image storage for the uploaded source photo
- Book-title candidate extraction from shelf photos, book-spine photos, and cover photos
- OCR-first recognition with vision-model fallback for ambiguous cases
- Candidate ranking and noise reduction
- Metadata lookup for the top candidates via the existing book-metadata search API
- Job polling for status and final results
- Frontend and API tests for the recognition flow

Out of scope:

- Recommendation generation
- User preference scoring
- Canonical catalog import
- Manual intake of a purchased copy
- Long-lived scan history
- Native mobile app support

## Story Slice

This story is the first recognition slice after photo upload.

The user flow is:

1. Open the scanning UI.
2. Upload a shelf photo or a single book-cover photo.
3. The API creates a recognition job and returns a job id immediately.
4. The frontend polls the job until the result is ready.
5. The app shows a list of likely book titles and their matched metadata.

The backend flow is:

1. Receive the image upload.
2. Store the source image temporarily.
3. Create a recognition job record.
4. Run OCR over the image.
5. Generate book-title candidates from the OCR text and layout.
6. Optionally use a vision-capable LLM only when OCR is ambiguous or low confidence.
7. Query the existing book-metadata search API for the strongest candidates.
8. Return normalized candidates together with their evidence and metadata matches.

## Recommended Approach

Use a job-based pipeline with Azure AI Vision OCR as the primary text extraction service and an Azure OpenAI vision model as a fallback for hard cases.

Why this is the right first slice:

- OCR is cheaper and better aligned with the actual problem of extracting visible book titles.
- A vision model is still available for cases where OCR alone is not enough, such as noisy cover art or partial occlusion.
- The job model fits the existing upload flow and keeps long-running work off the request path.
- The same interface can handle shelf photos, spine rows, and cover photos without separate upload endpoints.

Rejected alternatives:

- Synchronous recognition would make the mobile flow fragile and slow.
- Using only a vision LLM for every image would be more expensive and harder to tune.
- Splitting shelf, spine, and cover uploads into separate endpoints would add UI complexity without enough benefit.

## Input Model

The API should accept a single uploaded image and treat it as an unstructured recognition request.

The image may contain:

- a single book cover
- several book covers
- one or more rows of book spines
- a mixed shelf photo with titles, author names, slogans, and other visual noise

The story should not force the user to classify the image up front. The recognition pipeline should infer likely book-title regions from the content itself.

## Recognition Design

The recognition pipeline should be a layered heuristic process instead of a single black-box model.

### 1. OCR Extraction

Use OCR to extract all visible text along with bounding boxes, reading order, and confidence.

This layer is responsible for:

- reading text from shelf and cover photos
- preserving the spatial layout of text
- surfacing confidence values for later ranking

### 2. Candidate Generation

Turn OCR text into candidate book titles by grouping nearby tokens and short text spans.

This layer should preserve multiple plausible candidates rather than collapsing too early.

Candidate generation should account for:

- horizontal text on covers
- vertical or rotated text on book spines
- multiline title fragments
- repeated text that appears in multiple boxes

### 3. Noise Reduction and Ranking

Rank the candidates with heuristics that favor likely titles and down-rank obvious noise.

Useful signals include:

- title-like length
- fewer punctuation marks
- proximity to large or centered text
- repeated appearance across multiple OCR boxes
- vertical or narrow text regions for spines
- author lines, series blurbs, publisher text, ISBNs, and marketing copy as negative signals

The first version should optimize for recall over precision. It is acceptable to return many candidates and let the user confirm or remove them later.

### 4. Optional Vision-Model Fallback

When OCR confidence is low or when the page layout is unusually noisy, a vision-capable LLM can be asked to suggest additional candidate titles from the same image.

This fallback should:

- be optional
- not replace OCR
- only supplement the candidate set when needed

### 5. Metadata Enrichment

Take the strongest title candidates and query the existing book-metadata search API.

The recognition job should return the normalized metadata matches so the frontend can show the user a richer candidate list.

## Job Model

The job should be asynchronous and pollable.

Recommended states:

- `queued`
- `running`
- `succeeded`
- `failed`

The job result should include:

- the original upload reference
- the normalized candidate list
- the book-metadata matches for those candidates
- any provider or processing warnings

## API Design

Add a recognition API that creates a job and a result API that fetches its status.

Recommended endpoints:

- `POST /api/book-recognition-jobs`
- `GET /api/book-recognition-jobs/{jobId}`

The create endpoint should:

- accept a single image file
- create the recognition job immediately
- return the job id and initial status

The status endpoint should:

- return the job state
- return the candidate list once processing is complete
- surface failure details when recognition cannot complete

The API should not block the request until OCR or metadata lookup finishes.

## Frontend Design

The frontend should treat recognition as a background task.

Suggested UI states:

- Idle: show a capture button
- Uploading: show the file upload is in progress
- Processing: show a scanning state with polling
- Ready: show the recognized candidate list
- Error: show retry affordance

The first UI should support a useful number of candidates, not just one or two. The user should be able to review and narrow down a set of roughly 10 to 20 results from a single photo.

## Data Flow

1. User uploads a shelf or cover photo.
2. API stores the image temporarily and creates a recognition job.
3. Background processing runs OCR on the image.
4. OCR results are grouped into candidate book titles.
5. Candidates are ranked and deduplicated.
6. The top candidates are sent to the existing book-metadata search API.
7. The job is marked complete.
8. The frontend polls until the results are ready and then shows the candidate list.

## Error Handling

- Missing auth returns `401 Unauthorized`.
- Missing image returns `400 Bad Request`.
- Unsupported file type returns `400 Bad Request`.
- Oversized image returns `400 Bad Request`.
- Temporary storage failure returns `500 Internal Server Error`.
- OCR provider failure marks the job as failed and surfaces a recoverable error in the job result.
- Metadata search failure should not fail the whole job unless no candidate can be produced at all.

The system should prefer partial success over total failure:

- If OCR works but metadata lookup fails, return the OCR candidates.
- If the vision fallback fails, keep the OCR candidates.
- If some candidate lookups fail, keep the candidates that succeeded.

## Testing

Add coverage for:

- creating a recognition job from an uploaded image
- polling a job from queued to completed
- ranking and deduplicating OCR candidates
- returning metadata matches for the strongest title candidates
- falling back cleanly when the vision provider is unavailable
- rejecting missing, unsupported, or oversized uploads

## Dependencies

This story depends on:

- authenticated web sessions
- current family context
- the existing temporary image storage
- the book-metadata search API
- an OCR provider

It does not depend on recommendation scoring or canonical catalog import.

## Story Boundary

This story is complete when:

- a user can upload a shelf or cover photo,
- the server creates an async recognition job,
- OCR extracts a useful set of title candidates,
- the job can be polled to completion,
- and the results include normalized metadata lookups for the strongest candidates.
