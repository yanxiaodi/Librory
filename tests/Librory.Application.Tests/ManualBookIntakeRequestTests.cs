using Librory.Application.Intake;
using Librory.Domain.Models;
using Xunit;

namespace Librory.Application.Tests;

public class ManualBookIntakeRequestTests
{
    [Fact]
    public void Request_preserves_resolved_edition_member_and_optional_metadata()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");
        var edition = BookWork.Create("Charlotte's Web").AddEdition(isbn: "978-0-06-112495-2");

        var request = new ManualBookIntakeRequest(
            edition,
            member,
            BookCopyDuplicateStatus.ConfirmedUnique,
            "  Good  ",
            "  Thrift Shop  ",
            12.5m,
            "  Kids shelf  ",
            new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero),
            "  First copy after review  ");

        Assert.Same(edition, request.Edition);
        Assert.Same(member, request.OwningMember);
        Assert.Equal("Good", request.Condition);
        Assert.Equal("Thrift Shop", request.PurchaseStore);
        Assert.Equal(12.5m, request.PurchasePrice);
        Assert.Equal("Kids shelf", request.ShelfLocation);
        Assert.Equal(new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero), request.PurchasedAt);
        Assert.Equal(BookCopyDuplicateStatus.ConfirmedUnique, request.DuplicateStatus);
        Assert.Equal("First copy after review", request.IntakeNotes);
    }

    [Fact]
    public void Request_requires_duplicate_status_and_defaults_optional_metadata()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");
        var edition = BookWork.Create("Charlotte's Web").AddEdition(isbn: "978-0-06-112495-2");

        var request = new ManualBookIntakeRequest(edition, member, BookCopyDuplicateStatus.Unchecked);

        Assert.Equal(BookCopyDuplicateStatus.Unchecked, request.DuplicateStatus);
        Assert.Null(request.Condition);
        Assert.Null(request.PurchaseStore);
        Assert.Null(request.PurchasePrice);
        Assert.Null(request.ShelfLocation);
        Assert.Null(request.PurchasedAt);
        Assert.Null(request.IntakeNotes);
    }

    [Fact]
    public void Request_throws_when_edition_is_null()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");

        // null! exercises the runtime guard; the test is not about nullable flow analysis.
        Assert.Throws<ArgumentNullException>(() => new ManualBookIntakeRequest(null!, member, BookCopyDuplicateStatus.Unchecked));
    }

    [Fact]
    public void Request_throws_when_owning_member_is_null()
    {
        var edition = BookWork.Create("Charlotte's Web").AddEdition(isbn: "978-0-06-112495-2");

        Assert.Throws<ArgumentNullException>(() => new ManualBookIntakeRequest(edition, null!, BookCopyDuplicateStatus.Unchecked));
    }
}
