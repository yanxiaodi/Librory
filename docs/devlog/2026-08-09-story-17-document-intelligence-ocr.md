# 2026-08-09 Story 17 - Document Intelligence OCR Migration

## Summary

Started the migration from Azure Vision OCR terminology and adapter naming to Azure Document Intelligence.

## Changes

- Created branch `story-17-document-intelligence-ocr`
- Updated deployment and frontend docs to use Document Intelligence terminology
- Renamed the OCR configuration section to `Recognition:DocumentIntelligence`
- Renamed the OCR adapter class to `DocumentIntelligenceTextExtractionService`
- Added a formal design doc for the migration story

## Notes

- The recognition job boundary and Azure OpenAI vision fallback remain unchanged.
- The migration is intentionally narrow so the OCR provider can be swapped without changing the rest of the scan flow.
