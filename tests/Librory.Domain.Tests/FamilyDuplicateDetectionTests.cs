using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public class FamilyDuplicateDetectionTests
{
    [Fact]
    public void Family_detect_potential_duplicate_ignores_spacing_punctuation_and_case()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");
        var work = BookWork.Create("Charlotte's Web", "E. B. White");
        var edition = work.AddEdition(isbn: "978-0-06-112495-2", format: "Hardcover", publicationYear: 2006);
        var copy = family.AddBookCopy(edition, member);

        var result = family.DetectPotentialDuplicate("  charlotte s web!  ");

        Assert.True(result.HasPotentialDuplicate);
        Assert.Equal("charlotte s web!", result.CandidateTitle);
        Assert.Equal("CHARLOTTESWEB", result.NormalizedTitle);
        Assert.Equal("Capture ISBN or barcode information to confirm the edition.", result.FollowUpHint);
        Assert.Single(result.Matches);
        Assert.Equal(copy.Id, result.Matches[0].BookCopyId);
        Assert.Equal(edition.Id, result.Matches[0].BookEditionId);
        Assert.Equal(work.Id, result.Matches[0].BookWorkId);
        Assert.Equal("Charlotte's Web", result.Matches[0].Title);
        Assert.Equal("978-0-06-112495-2", result.Matches[0].Isbn);
        Assert.Equal("Hardcover", result.Matches[0].Format);
        Assert.Equal(2006, result.Matches[0].PublicationYear);
    }

    [Fact]
    public void Family_detect_potential_duplicate_returns_no_match_for_different_title()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");
        var edition = BookWork.Create("Charlotte's Web").AddEdition(isbn: "978-0-06-112495-2");
        family.AddBookCopy(edition, member);

        var result = family.DetectPotentialDuplicate("Matilda");

        Assert.False(result.HasPotentialDuplicate);
        Assert.Empty(result.Matches);
        Assert.Null(result.FollowUpHint);
    }

    [Fact]
    public void Family_detect_potential_duplicate_accepts_book_edition_for_intake()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");
        var work = BookWork.Create("Matilda", "Roald Dahl");
        var edition = work.AddEdition(isbn: "978-0-14-241037-0");
        family.AddBookCopy(edition, member);

        var result = family.DetectPotentialDuplicate(edition);

        Assert.True(result.HasPotentialDuplicate);
        Assert.Single(result.Matches);
        Assert.Equal("Matilda", result.Matches[0].Title);
        Assert.Equal("978-0-14-241037-0", result.Matches[0].Isbn);
    }
}
