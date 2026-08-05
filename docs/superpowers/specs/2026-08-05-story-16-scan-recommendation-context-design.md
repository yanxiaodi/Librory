# Story 16 Design: Scan Recommendation Context

## Goal

Connect family membership and recommendation profiles to scanning without making AI orchestration part of this story. A scan should have one clear target member and should adapt to the language found in the scanned books.

## Scope

In scope:

- Select one target member for each scan.
- Default the target to the current member.
- Allow the user to choose another active family member whose profile is enabled for use.
- Persist the target member id on the scan session or recognition job.
- Infer a temporary language context from recognized candidates and metadata.
- Use the context for later recommendation scoring without changing the saved profile.
- Return enough context for the frontend to explain which member and language were used.

Out of scope:

- Multiple target members in one scan.
- Family-wide aggregate profiles.
- Editing profiles during a scan.
- AI recommendation generation.
- Permanent mutation of a member's preferred book languages based on a scan.

## Scan Context

Each scan has:

- `TargetMemberId`, defaulting to the current member
- the target member display name in the response
- an inferred language context once candidates are available
- an indication of whether the target profile was available and used

The target is selected once for the scan. It is not changed automatically while a job is processing.

If the target member has no usable profile, recognition, metadata, and duplicate checks continue; personalized recommendation fields are omitted or marked unavailable.

## Language Adaptation

Language is evaluated per candidate from normalized metadata when available. The scan can also infer a dominant language when most recognized candidates agree.

- A dominant scan language temporarily outranks the target profile's default language preference.
- A mixed-language shelf scores each candidate using its own language.
- Unknown language does not remove a candidate; it lowers confidence in language-specific scoring.
- The inferred language context is stored with the scan result for explanation and repeatability.
- The member's long-term profile is never modified by automatic inference.

## API Direction

The recognition-job and scan-session create requests should accept an optional `targetMemberId`.

- omitted target: use the current member
- supplied target: require an active family member; when the target differs from the caller, require `UseInFamilyRecommendations = true` and either family visibility or administrator authorization
- foreign, inactive, or unauthorized target: reject with a clear validation response

The response should include target-member context and language-context state without returning private profile notes.

## Acceptance Criteria

- A scan can be created without specifying a target member.
- The current member is used by default.
- A user can select one eligible family member as the target.
- A scan stores the selected target and does not silently switch it.
- Recognition and duplicate results continue when no profile exists.
- The inferred scan language can override long-term language ordering without changing the profile.
- Mixed-language scans score candidates independently.
- Unknown language remains visible and does not cause the scan to fail.
- The API rejects targets outside the active family or targets that are inactive or disabled for recommendations.
- The frontend can show which member and language context were used.
