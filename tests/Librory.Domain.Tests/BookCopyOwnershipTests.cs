using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public class BookCopyOwnershipTests
{
    [Fact]
    public void Family_add_book_copy_registers_family_member_and_edition()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice", MemberRole.Admin);
        var work = BookWork.Create("Charlotte's Web", "E. B. White");
        var edition = work.AddEdition(isbn: "978-0-06-112495-2", format: "Hardcover", publicationYear: 2006);

        var copy = family.AddBookCopy(
            edition,
            member,
            condition: "Good",
            purchaseStore: "Thrift Shop",
            purchasePrice: 12.5m,
            shelfLocation: "Kids shelf",
            purchasedAt: new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero));

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
    }

    [Fact]
    public void Family_add_book_copy_uses_null_for_optional_fields()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");
        var edition = BookWork.Create("Charlotte's Web").AddEdition(isbn: "978-0-06-112495-2");

        var copy = family.AddBookCopy(edition, member);

        Assert.Null(copy.Condition);
        Assert.Null(copy.PurchaseStore);
        Assert.Null(copy.PurchasePrice);
        Assert.Null(copy.ShelfLocation);
        Assert.Null(copy.PurchasedAt);
    }

    [Fact]
    public void Family_add_book_copy_normalizes_whitespace_fields_to_null()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");
        var edition = BookWork.Create("Charlotte's Web").AddEdition(isbn: "978-0-06-112495-2");

        var copy = family.AddBookCopy(
            edition,
            member,
            condition: "   ",
            purchaseStore: "\t",
            shelfLocation: "  ");

        Assert.Null(copy.Condition);
        Assert.Null(copy.PurchaseStore);
        Assert.Null(copy.ShelfLocation);
    }

    [Fact]
    public void Family_add_book_copy_throws_when_member_is_from_another_family()
    {
        var firstFamily = Family.Create("First");
        var secondFamily = Family.Create("Second");
        var member = firstFamily.AddMember("Alice");
        var edition = BookWork.Create("Charlotte's Web").AddEdition(isbn: "978-0-06-112495-2");

        var exception = Assert.Throws<InvalidOperationException>(() => secondFamily.AddBookCopy(edition, member));

        Assert.Equal("Member must belong to the same family as the copy.", exception.Message);
        Assert.Empty(secondFamily.BookCopies);
    }

    [Fact]
    public void BookCopy_create_throws_when_edition_is_not_attached_to_a_work()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");
        var edition = new BookEdition();

        var exception = Assert.Throws<InvalidOperationException>(() => BookCopy.Create(edition, family, member));

        Assert.Equal("Edition must belong to a work before a copy can be created.", exception.Message);
    }

    [Fact]
    public void BookCopy_create_throws_when_edition_is_not_attached_to_a_work_directly()
    {
        Assert.Throws<ArgumentNullException>(() => BookCopy.Create(null!, Family.Create("The Yans"), new Member()));
        Assert.Throws<ArgumentNullException>(() => BookCopy.Create(new BookEdition(), null!, new Member()));
        Assert.Throws<ArgumentNullException>(() => BookCopy.Create(new BookEdition(), Family.Create("The Yans"), null!));
    }
}
