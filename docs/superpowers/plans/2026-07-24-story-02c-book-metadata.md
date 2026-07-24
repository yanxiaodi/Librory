# Story 02c Book Metadata Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a minimal shared metadata provenance model and wire it into `BookWork` and `BookEdition` so the domain can carry localized metadata and source confidence without expanding into persistence or API work.

**Architecture:** Keep the change domain-only. Add one shared immutable value object for provenance, then attach small metadata/provenance pairs to the work and edition entities where those facts naturally belong. Do not touch `BookCopy`, application services, or database mapping in this slice.

**Tech Stack:** C# / .NET 10, xUnit

---

### Task 1: Add the shared metadata provenance value object

**Files:**
- Create: `src/Librory.Domain/Models/MetadataProvenance.cs`
- Test: `tests/Librory.Domain.Tests/MetadataProvenanceTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public class MetadataProvenanceTests
{
    [Fact]
    public void Metadata_provenance_preserves_source_values()
    {
        var capturedAt = new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);

        var provenance = new MetadataProvenance(
            Source: "google-books",
            SourceId: "9780061124952",
            Confidence: 0.92m,
            CapturedAt: capturedAt);

        Assert.Equal("google-books", provenance.Source);
        Assert.Equal("9780061124952", provenance.SourceId);
        Assert.Equal(0.92m, provenance.Confidence);
        Assert.Equal(capturedAt, provenance.CapturedAt);
    }

    [Fact]
    public void Metadata_provenance_allows_optional_values_to_be_null()
    {
        var provenance = new MetadataProvenance();

        Assert.Null(provenance.Source);
        Assert.Null(provenance.SourceId);
        Assert.Null(provenance.Confidence);
        Assert.Null(provenance.CapturedAt);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests\Librory.Domain.Tests\Librory.Domain.Tests.csproj --filter MetadataProvenanceTests`
Expected: fail because `MetadataProvenance` does not exist yet.

- [ ] **Step 3: Write the minimal implementation**

```csharp
namespace Librory.Domain.Models;

public sealed record MetadataProvenance(
    string? Source = null,
    string? SourceId = null,
    decimal? Confidence = null,
    DateTimeOffset? CapturedAt = null);
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test tests\Librory.Domain.Tests\Librory.Domain.Tests.csproj --filter MetadataProvenanceTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Librory.Domain/Models/MetadataProvenance.cs tests/Librory.Domain.Tests/MetadataProvenanceTests.cs
git commit -m "Add metadata provenance value object"
```

### Task 2: Add work-level metadata hooks

**Files:**
- Modify: `src/Librory.Domain/Models/BookWork.cs`
- Test: `tests/Librory.Domain.Tests/BookWorkMetadataTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public class BookWorkMetadataTests
{
    [Fact]
    public void Book_work_can_store_summary_and_provenance()
    {
        var work = BookWork.Create("Charlotte's Web", "E. B. White");
        var provenance = new MetadataProvenance(
            Source: "library-of-congress",
            SourceId: "2011029476",
            Confidence: 1.0m,
            CapturedAt: new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero));

        work.Summary = new LocalizedText("A pig named Wilbur...", "一只叫威尔伯的猪...");
        work.SummaryProvenance = provenance;
        work.CanonicalAuthorProvenance = provenance;

        Assert.Equal("A pig named Wilbur...", work.Summary!.English);
        Assert.Equal("一只叫威尔伯的猪...", work.Summary!.Chinese);
        Assert.Same(provenance, work.SummaryProvenance);
        Assert.Same(provenance, work.CanonicalAuthorProvenance);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests\Librory.Domain.Tests\Librory.Domain.Tests.csproj --filter BookWorkMetadataTests`
Expected: fail because `SummaryProvenance` and `CanonicalAuthorProvenance` do not exist yet.

- [ ] **Step 3: Write the minimal implementation**

```csharp
namespace Librory.Domain.Models;

public sealed class BookWork
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string CanonicalTitle { get; set; } = string.Empty;
    public string? CanonicalAuthor { get; set; }
    public LocalizedText? Summary { get; set; }
    public MetadataProvenance? SummaryProvenance { get; set; }
    public MetadataProvenance? CanonicalAuthorProvenance { get; set; }
    private readonly List<BookEdition> _editions = [];
    public IReadOnlyList<BookEdition> Editions => _editions;
}
```

Leave the existing `Create`, `AddEdition`, and `RegisterEdition` members unchanged.

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test tests\Librory.Domain.Tests\Librory.Domain.Tests.csproj --filter BookWorkMetadataTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Librory.Domain/Models/BookWork.cs tests/Librory.Domain.Tests/BookWorkMetadataTests.cs
git commit -m "Add work metadata hooks"
```

### Task 3: Add edition-level metadata hooks

**Files:**
- Modify: `src/Librory.Domain/Models/BookEdition.cs`
- Test: `tests/Librory.Domain.Tests/BookEditionMetadataTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public class BookEditionMetadataTests
{
    [Fact]
    public void Book_edition_can_store_subtitle_and_provenance()
    {
        var work = BookWork.Create("Charlotte's Web");
        var edition = work.AddEdition(isbn: "978-0-06-112495-2", format: "Hardcover", publicationYear: 2006);
        var provenance = new MetadataProvenance(
            Source: "openlibrary",
            SourceId: "OL123M",
            Confidence: 0.84m,
            CapturedAt: new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero));

        edition.Subtitle = new LocalizedText("Collector's Edition", "收藏版");
        edition.SubtitleProvenance = provenance;
        edition.PublicationYearProvenance = provenance;

        Assert.Equal("Collector's Edition", edition.Subtitle!.English);
        Assert.Equal("收藏版", edition.Subtitle!.Chinese);
        Assert.Same(provenance, edition.SubtitleProvenance);
        Assert.Same(provenance, edition.PublicationYearProvenance);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests\Librory.Domain.Tests\Librory.Domain.Tests.csproj --filter BookEditionMetadataTests`
Expected: fail because the edition metadata properties do not exist yet.

- [ ] **Step 3: Write the minimal implementation**

```csharp
namespace Librory.Domain.Models;

public sealed class BookEdition
{
    public Guid Id { get; private set; }
    public Guid BookWorkId { get; private set; }
    public string? Isbn { get; set; }
    public string? Format { get; set; }
    public int? PublicationYear { get; set; }
    public LocalizedText? Subtitle { get; set; }
    public MetadataProvenance? SubtitleProvenance { get; set; }
    public MetadataProvenance? PublicationYearProvenance { get; set; }
    public BookWork BookWork { get; private set; } = null!;
    public bool IsAttachedToWork => BookWorkId != Guid.Empty;

    public BookEdition()
    {
        Id = Guid.NewGuid();
    }

    public void AssignToWork(BookWork work)
    {
        ArgumentNullException.ThrowIfNull(work);

        if (IsAttachedToWork && BookWorkId != work.Id)
        {
            throw new InvalidOperationException("Edition already belongs to a different work.");
        }

        BookWorkId = work.Id;
        BookWork = work;
        work.RegisterEdition(this);
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test tests\Librory.Domain.Tests\Librory.Domain.Tests.csproj --filter BookEditionMetadataTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Librory.Domain/Models/BookEdition.cs tests/Librory.Domain.Tests/BookEditionMetadataTests.cs
git commit -m "Add edition metadata hooks"
```

### Task 4: Run the full domain suite and prepare the PR

**Files:**
- None

- [ ] **Step 1: Run the full domain test suite**

Run: `dotnet test tests\Librory.Domain.Tests\Librory.Domain.Tests.csproj`
Expected: all domain tests pass.

- [ ] **Step 2: Inspect the diff for scope**

Run: `git status --short`
Expected: only the new provenance, work metadata, edition metadata, and related tests should be modified.

- [ ] **Step 3: Push and open the PR**

Run:

```bash
git push -u origin codex/story-02c-book-metadata
gh pr create --title "Story 02c: add book metadata and provenance hooks" --body "Add shared metadata provenance and work/edition metadata hooks for story-02c."
```

Expected: a small PR for the story-02c slice.

## Coverage Check

This plan covers:

- shared provenance value object
- work-level metadata and provenance
- edition-level metadata and provenance
- domain tests for optional/default behavior
- a final full-suite validation step

It intentionally does not cover persistence or application-layer wiring because the spec kept those out of scope.
