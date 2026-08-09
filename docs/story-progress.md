# Story Progress Table

## Unfinished First

| Story | Title | Status | Code evidence | What is still missing |
| --- | --- | --- | --- | --- |
| 07 | Wishlist | Not started | Wishlist domain, API, and tests exist | No story-specific implementation slice or delivery note yet |
| 08 | Localization-Aware Shaping | Not started | Localization docs exist | No implementation slice yet |
| 09 | Frontend Shell and Theme Switching | Not started | Frontend shell docs exist | No implementation slice yet |

## Already Implemented

| Story | Title | Status | Code evidence |
| --- | --- | --- | --- |
| 01 | Identity, Family, and Login | Done | Login/auth flow, family bootstrap, and tests are present |
| 02 | Core Book Domain Model | Done | Work/edition/copy domain and tests are present |
| 03 | Manual Book Intake | Done | `ManualBookIntakeRecorder` and tests are present |
| 04 | Shelf Scan Sessions | Done | `ScanSessionService`, scan endpoints, and tests are present |
| 04b | Candidate Correction | Done | `ScanCandidate.ApplyCorrection` and `ScanSession.CorrectCandidate` are present |
| 05 | Duplicate Detection | Done | Duplicate detection domain, scan review warnings, and tests are present |
| 06 | Recommendation Profiles | Done | Recommendation profile domain, API, and tests are present |
| 13 | Book Recognition Results Web / Job | Done | Recognition job, ranked candidates, and metadata enrichment are present |
| 15 | Member Recommendation Profiles / Web | Done | Recommendation profile web UI and API are present |
| 16 | Scan Recommendation Context / Web | Done | Scan target selection and recommendation context are present |
| 17 | Document Intelligence OCR Migration | In progress | OCR adapter and configuration have been renamed to Document Intelligence terminology |
| 10 | API and Persistence Foundation | Done | API, EF Core, migrations, and integration tests are present |
| 11 | PostgreSQL Test Infrastructure | Done | PostgreSQL-backed test infrastructure is present |
| 12 | External Metadata Providers and Canonical Import | Done | External metadata import flow and tests are present |
| 14 | Family Membership and Invitations | Done | Family management, invitations, and acceptance are present |

## How To Use

- Check the unfinished section first when deciding the next story.
- Use the code evidence column to verify the status against the repository, not just the story docs.
- If a story has code evidence but no delivery note, it is still treated as done here only when the code and tests are present.
- Scan-to-intake is already possible as separate steps: create a scan session, correct or resolve candidates, and record a manual intake.
- AI recommendation work is represented by stories 06, 13, 15, and 16, but it is not a single end-to-end recommendation engine yet.
