using Librory.Application.Scanning;
using Librory.Domain.Models;
using Xunit;

namespace Librory.Application.Tests;

public class ScanOutputMappingTests
{
    [Fact]
    public void Scan_session_dto_factory_enriches_candidates_with_duplicate_warning()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");
        var work = BookWork.Create("Charlotte's Web", "E. B. White");
        var edition = work.AddEdition(isbn: "978-0-06-112495-2", format: "Hardcover");
        family.AddBookCopy(edition, member);

        var session = family.StartScanSession("shelf-photo.jpg");
        var candidate = ScanCandidate.Create(
            "  charlotte s web!  ",
            confidenceLabel: "High",
            author: "E. B. White",
            recommendationScore: 0.94m);
        session.AddCandidate(candidate);

        var dto = ScanSessionDtoFactory.Create(family, session);

        Assert.Equal("shelf-photo.jpg", dto.ShelfPhotoPath);
        Assert.Single(dto.Candidates);
        Assert.True(dto.Candidates[0].IsAlreadyOwned);
        Assert.Equal("Capture ISBN or barcode information to confirm the edition.", dto.Candidates[0].DuplicateMessage);
    }

    [Fact]
    public void Scan_candidate_dto_factory_keeps_existing_duplicate_message_when_present()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");
        var work = BookWork.Create("Matilda", "Roald Dahl");
        var edition = work.AddEdition(isbn: "978-0-14-241037-0");
        family.AddBookCopy(edition, member);

        var candidate = ScanCandidate.Create(
            "Matilda",
            confidenceLabel: "Medium",
            author: "Roald Dahl",
            recommendationScore: 0.81m,
            duplicateMessage: "Manual review already noted this one.");

        var dto = ScanCandidateDtoFactory.Create(family, candidate);

        Assert.True(dto.IsAlreadyOwned);
        Assert.Equal("Manual review already noted this one.", dto.DuplicateMessage);
    }

    [Fact]
    public void Scan_candidate_dto_factory_leaves_duplicate_flags_clear_when_no_match_exists()
    {
        var family = Family.Create("The Yans");
        var candidate = ScanCandidate.Create(
            "Some Other Book",
            confidenceLabel: "Low",
            recommendationScore: 0.22m);

        var dto = ScanCandidateDtoFactory.Create(family, candidate);

        Assert.False(dto.IsAlreadyOwned);
        Assert.Null(dto.DuplicateMessage);
    }
}
