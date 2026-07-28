# Story 01 Design: Shelf Photo Upload

## Goal

Let a signed-in user on the web app take or select a shelf photo from a phone browser, upload it, and immediately enter a scanning state while the server stores the image temporarily and creates a scan session for downstream processing.

## Scope

In scope:

- Web-only photo capture and upload
- Temporary image storage
- Scan session creation
- Immediate "scanning" UI state after upload
- Short-lived retention for incomplete or failed uploads
- Daily cleanup of expired temporary scan data
- API and frontend tests for the upload path

Out of scope:

- OCR
- LLM-based recognition
- Book metadata lookup
- Recommendation generation
- Scan history
- Native mobile app support
- User-visible review of uploaded source images after successful scan completion

## Story Slice

This story is the first slice of the scanning flow.

The user action is simple:

1. Open the Scans page.
2. Tap Scan.
3. Take a photo or choose one from the phone.
4. Upload the image.
5. See the app enter a scanning state.

The backend action is also narrow:

1. Receive the image upload.
2. Store the file temporarily.
3. Create a scan session linked to the current family.
4. Return the scan session id and current status.

The later scanning steps will consume the stored image and update the session.

## Recommended Approach

Use direct multipart upload to the API from the web client.

Why this is the right first slice:

- It keeps the first implementation simple.
- It works well for mobile browsers without a native app.
- It avoids introducing upload credentials, blob signing, or a separate storage client in the frontend.
- It gives us a clean seam for later OCR and LLM processing.

Rejected alternatives:

- Direct browser upload to object storage is more scalable, but too much infrastructure for the first slice.
- A synchronous "upload and wait for results" request would make the mobile experience fragile and slow.

## Frontend Design

The Scans page should become the entry point for shelf capture.

UI states:

- Idle: show a prominent Scan action.
- Uploading: show progress or a busy state.
- Scanning: confirm the image was received and the system is processing it.
- Error: show retry affordance if upload fails.

Capture behavior:

- Use a file input configured for images.
- Prefer mobile camera capture where the browser supports it.
- Accept a single image for now.

The page does not need a gallery, history list, or previous-session recovery in this story.

## Backend Design

Add a dedicated upload endpoint that accepts the image file and creates the temporary scan session.

Recommended responsibilities:

- Validate auth and family context.
- Validate file presence, size, and mime type.
- Persist the image to a temporary storage location.
- Create a scan session record that points at the stored file.
- Return the new session in a response that lets the frontend show the scanning state.

The existing scan-session model already provides a place to store a temporary shelf photo reference and expiry metadata. The story should extend that model rather than invent a separate long-lived book workflow.

## Retention and Cleanup

Temporary scan data should be short-lived.

Recommended policy:

- Successful scans: delete the original uploaded image as soon as downstream processing no longer needs it.
- Incomplete or failed scans: retain the temporary image and session for 24 hours.
- Cleanup job: run once per day and delete expired temporary data.

The cleanup job should only delete expired temporary scan data. It should not attempt OCR retries, repair, or other business logic.

## Data Flow

1. User taps Scan on the web page.
2. Browser captures or selects an image.
3. Browser uploads the image to the API.
4. API stores the image temporarily.
5. API creates a scan session and returns its id.
6. Frontend switches to a scanning state.
7. Later stories consume the image and populate the session with recognized candidates.

## Error Handling

- Missing auth returns `401 Unauthorized`.
- Missing image returns `400 Bad Request`.
- Unsupported file type returns `400 Bad Request`.
- Oversized image returns `400 Bad Request`.
- Storage failure returns `500 Internal Server Error`.
- If the session cannot be created after the image is stored, the upload should be cleaned up before the request exits.

The frontend should treat upload failure as recoverable and keep the scan action visible.

## Testing

Add coverage for:

- uploading a valid shelf photo
- rejecting a missing or invalid file
- creating a scan session for the current family
- scoping the upload to the current family context
- removing expired temporary scan data
- rendering the upload state on the Scans page

## Dependencies

This story depends on:

- authenticated web sessions
- current family context
- the existing scanning domain model
- a temporary file storage location

It does not depend on OCR or third-party book APIs.

## Story Boundary

This story is complete when:

- a phone browser can upload a shelf photo,
- the backend stores it temporarily,
- a scan session is created,
- the UI shows a scanning state,
- and expired temporary data is cleaned up automatically.

