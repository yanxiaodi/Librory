# 2026-08-04 Story 13 Book Recognition Job

- Added the recognition job API and wired the scan page to create a job, poll for completion, and render the resulting candidates.
- Updated the backend story map to mark `story-13` as delivered.
- Updated the frontend integration guide so the recognition job flow is listed as ready.
- Verified with `dotnet test tests/Librory.Domain.Tests/Librory.Domain.Tests.csproj --filter FullyQualifiedName~ScanSessionTests -v minimal` and `dotnet test tests/Librory.Api.Tests/Librory.Api.Tests.csproj --filter FullyQualifiedName~Scan_session -v minimal`.
