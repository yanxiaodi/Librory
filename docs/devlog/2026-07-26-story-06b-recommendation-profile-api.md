# 2026-07-26 Story 06b Recommendation Profile API

- Added a family-scoped recommendation profile API for the current signed-in member.
- Persisted recommendation profiles in PostgreSQL with a one-profile-per-member unique constraint.
- Stored favorite authors, genres, and styles as serialized list values so partial updates preserve existing preferences.
- Kept the API member-scoped and separate from recommendation scoring or AI workflow logic.
- Verified with `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj` and `dotnet test tests/Librory.Application.Tests/Librory.Application.Tests.csproj`.
