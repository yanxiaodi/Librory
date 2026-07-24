using Librory.Application.Intake;
using Librory.Domain.Models;
using Xunit;

namespace Librory.Application.Tests;

public class ManualBookIntakeRecorderTests
{
    [Fact]
    public void Record_creates_a_book_copy_on_the_current_family()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice", MemberRole.Admin);
        var edition = BookWork.Create("Charlotte's Web", "E. B. White")
            .AddEdition(isbn: "978-0-06-112495-2", format: "Hardcover", publicationYear: 2006);
        var request = new ManualBookIntakeRequest(
            edition,
            member,
            BookCopyDuplicateStatus.ConfirmedUnique,
            "Good",
            "Thrift Shop",
            12.5m,
            "Kids shelf",
            new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero),
            "First copy after review");

        var copy = ManualBookIntakeRecorder.Record(family, request);

        Assert.Single(family.BookCopies);
        Assert.Same(copy, family.BookCopies[0]);
        Assert.Equal(family.Id, copy.FamilyId);
        Assert.Same(family, copy.Family);
        Assert.Equal(member.Id, copy.MemberId);
        Assert.Same(member, copy.Member);
        Assert.Equal(edition.Id, copy.BookEditionId);
        Assert.Same(edition, copy.BookEdition);
        Assert.Equal("Good", copy.Condition);
        Assert.Equal("Thrift Shop", copy.PurchaseStore);
        Assert.Equal(12.5m, copy.PurchasePrice);
        Assert.Equal("Kids shelf", copy.ShelfLocation);
        Assert.Equal(new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero), copy.PurchasedAt);
        Assert.Equal(BookCopyDuplicateStatus.ConfirmedUnique, copy.DuplicateStatus);
        Assert.Equal("First copy after review", copy.IntakeNotes);
    }

    [Fact]
    public void Record_uses_domain_validation_for_cross_family_members()
    {
        var firstFamily = Family.Create("First");
        var secondFamily = Family.Create("Second");
        var member = firstFamily.AddMember("Alice");
        var edition = BookWork.Create("Charlotte's Web").AddEdition(isbn: "978-0-06-112495-2");
        var request = new ManualBookIntakeRequest(edition, member, BookCopyDuplicateStatus.Unchecked);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ManualBookIntakeRecorder.Record(secondFamily, request));

        Assert.Equal("Member must belong to the same family as the copy.", exception.Message);
        Assert.Empty(secondFamily.BookCopies);
    }

    [Fact]
    public void Record_throws_when_family_is_null()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");
        var edition = BookWork.Create("Charlotte's Web").AddEdition(isbn: "978-0-06-112495-2");
        var request = new ManualBookIntakeRequest(edition, member, BookCopyDuplicateStatus.Unchecked);

        // null! exercises the runtime guard; the test is not about nullable flow analysis.
        Assert.Throws<ArgumentNullException>(() => ManualBookIntakeRecorder.Record(null!, request));
    }

    [Fact]
    public void Record_throws_when_request_is_null()
    {
        var family = Family.Create("The Yans");

        Assert.Throws<ArgumentNullException>(() => ManualBookIntakeRecorder.Record(family, null!));
    }
}
