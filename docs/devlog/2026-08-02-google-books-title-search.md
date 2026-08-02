# 2026-08-02 Google Books Title Search

- Added a book metadata search slice that queries Google Books by title and returns normalized metadata instead of raw provider payloads.
- Introduced an application-level metadata search abstraction so later OCR and recommendation work can reuse the same interface.
- Kept Google Books integration behind an infrastructure service with a configurable API key and a narrow normalized response shape.
- Documented the new `/api/book-metadata/search` endpoint in the API reference, frontend integration guide, and deployment notes.
- Verified the book metadata tests pass together with the existing scan upload and cleanup tests.
