# 2026-07-26 Story 03b Manual Intake API

- Added a family-scoped manual intake API that creates a `BookCopy` from a resolved edition and reuses the current member as the owner.
- Returned duplicate detection summary data alongside the created copy so later UI work can surface the warning without re-running domain logic.
- Added a stable fetch route for the created copy so the new resource has a predictable URL.
- Kept ISBN/title lookup out of this slice; external metadata providers remain a separate follow-up story.
- Verified with `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj` and `dotnet test tests/Librory.Application.Tests/Librory.Application.Tests.csproj`.
