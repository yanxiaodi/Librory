# 2026-07-24 Story 05 Duplicate Detection

- Scoped duplicate detection to normalized title matches so the warning stays simple and fast.
- Ignored capitalization, whitespace, and punctuation when comparing titles so scan input and manual intake can both trigger the same warning.
- Added application-level result mappers for manual intake and scan session output so the UI can surface the warning without changing domain behavior.
- Kept duplicate detection as a warning-only signal; it does not block save or scan flow.
- Verified with `dotnet test tests/Librory.Domain.Tests/Librory.Domain.Tests.csproj` and `dotnet test tests/Librory.Application.Tests/Librory.Application.Tests.csproj`.
