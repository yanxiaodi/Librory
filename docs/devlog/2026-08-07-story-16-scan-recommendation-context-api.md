# 2026-08-07 Story 16 Scan Recommendation Context API

## Delivered

- Added family-scoped scan target selection with current-member defaulting and administrator/family-visible profile authorization.
- Persisted the selected target member, profile availability/use flags, candidate detected language, and temporary inferred language context.
- Added strict-dominant-language and mixed-language handling without mutating saved recommendation profiles.
- Exposed target and language context in scan responses while keeping profile notes private.
- Preserved legacy scan creation and cleanup behavior for sessions created without the new target context.

## Boundary

This backend slice prepares scan context for later recommendation scoring. It does not generate AI recommendations, aggregate multiple targets, or add frontend behavior.

## Validation

- Domain scan-session tests pass.
- Application scan mapping tests pass.
- API scan tests pass, including cleanup compatibility.
- Full solution tests and build are run before the PR is opened.
