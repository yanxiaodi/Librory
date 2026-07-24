# 2026-07-24 Story 04b Candidate Correction

- Scoped `story-04b` to in-place domain correction for a single scan candidate.
- Added `ScanCandidate.ApplyCorrection(...)` so corrected title, author, confidence, duplicate, and recommendation fields can be updated without changing identity.
- Added `ScanSession.CorrectCandidate(...)` so the session can update one candidate while preserving the others and its expiration state.
- Updated `docs/backend-story-map.md` and `docs/data-model.md` to make the in-place correction boundary explicit.
- Verified with `dotnet test tests/Librory.Domain.Tests/Librory.Domain.Tests.csproj --filter "ScanCandidateTests|ScanSessionTests"`.
