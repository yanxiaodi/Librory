## 🟡 Code Review: Add shared contracts and primitives

### 🔴 Blocking

#### 1. Tests csproj have developer-local absolute paths in `RestoreSources`

`tests/Librory.Domain.Tests/Librory.Domain.Tests.csproj` and `tests/Librory.Application.Tests/Librory.Application.Tests.csproj` contain hardcoded `RestoreSources` pointing to a local NuGet cache path (`C:\Users\xiaodi.yan\.nuget\packages\...`).

This will cause `dotnet restore` to fail on any other machine or CI.

```diff
  <PropertyGroup>
    <IsPackable>false</IsPackable>
-   <RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources>
-   <RestoreSources>C:\Users\xiaodi.yan\.nuget\packages\...</RestoreSources>
  </PropertyGroup>
```

If offline/private packages are needed, use a project-level `nuget.config` instead.

#### 2. Test projects are not added to `Librory.sln`

The solution file still only contains the original 6 projects. The two new test projects won't be built by `dotnet build Librory.sln` and won't appear in Visual Studio.

Please add both `tests/Librory.Domain.Tests/Librory.Domain.Tests.csproj` and `tests/Librory.Application.Tests/Librory.Application.Tests.csproj` to the solution.

---

### 🟡 Non-blocking (Suggestions)

#### 3. `WishlistItem` ownership semantics vs story map

`WishlistItem` is placed under `Family` (shared family library scope), but story-map Epic 8 uses "As a **user**, I want to add a book to a wishlist" which reads as personal. This is fine as a shared wishlist, but it may be ambiguous. Future PR should clarify:
- Is the wishlist family-wide or per-member?
- If family-wide, consider adding `CreatedByMemberId` to track who added it.

#### 4. `CorrectionRequest` allows both fields to be null

```cs
public sealed record CorrectionRequest(string? RescannedPhotoPath, string? ManualTitle);
```

`new CorrectionRequest(null, null)` is syntactically valid but semantically invalid. Consider adding a runtime guard (e.g. in the service implementation or a factory method) to enforce that at least one correction method is provided.

---

### ✅ Well done

- **MemberRole safe default**: `Member = 0` with `= MemberRole.Member` initializer avoids accidental admin elevation.
- **Aggregate root design**: `Family` collections (`Members`, `BookCopies`, `WishlistItems`, `RecommendationProfiles`, `ScanSessions`) are complete and consistent with `data-model.md`.
- **Navigation properties**: `BookCopy.Family`/`Member`/`BookEdition` and `WishlistItem.BookWork`/`BookEdition` fill the gaps from the skeleton PR.
- **ScanCandidateDto separation**: `RecommendationScore` (numeric) vs `ConfidenceLabel` (discrete) respects PRD requirement for clearly marking uncertain matches.
- **IScanSessionService contract**: `StartShelfScanAsync` + `ApplyCorrectionAsync` cleanly covers the core scanning flow defined in the story map.
- **Guard clause in DI**: `ArgumentNullException.ThrowIfNull(services)` is the correct pattern.
- **Immutable DTOs**: All contracts use `sealed record` with value semantics.

**Fix blocking 1 and 2, then this is good to merge.**
