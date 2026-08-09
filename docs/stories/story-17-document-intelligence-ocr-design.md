# Story 17 Design: Document Intelligence OCR Migration

## Goal

Replace the legacy Azure Vision OCR dependency with Azure Document Intelligence for book recognition text extraction, while keeping the existing recognition job and vision fallback flow intact.

## Scope

In scope:

- Replace the OCR adapter with a Document Intelligence-backed implementation
- Rename configuration keys and related code to Document Intelligence terminology
- Update deployment and frontend integration docs to match the new OCR provider
- Keep the recognition job boundary, ranking, and vision fallback behavior unchanged

Out of scope:

- Reworking the recognition job pipeline
- Changing candidate ranking logic
- Changing the Azure OpenAI vision fallback flow
- Changing scan-session or manual-intake behavior

## Design

The migration should be a narrow provider swap.

The application should continue to:

- accept a shelf or cover photo
- extract text asynchronously
- rank candidate titles
- enrich candidates with metadata
- fall back to vision interpretation when needed

Only the OCR provider and its configuration should change.

## Behavior

- The OCR adapter calls Azure Document Intelligence Read
- The recognition pipeline consumes the extracted text blocks as before
- The frontend still polls the recognition job and renders candidates the same way

## Testing

Add coverage for:

- Document Intelligence configuration binding
- OCR adapter request and response parsing
- recognition pipeline behavior with the new adapter name
- documentation references to the new provider terminology
