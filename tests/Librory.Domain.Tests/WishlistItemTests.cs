using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public class WishlistItemTests
{
    [Fact]
    public void Family_add_wishlist_item_registers_family_item_and_references_work_and_edition()
    {
        var family = Family.Create("The Yans");
        var work = BookWork.Create("Charlotte's Web", "E. B. White");
        var edition = work.AddEdition(isbn: "978-0-06-112495-2", format: "Hardcover", publicationYear: 2006);

        var item = family.AddWishlistItem(
            "Charlotte's Web",
            "  E. B. White  ",
            work,
            edition);

        Assert.Single(family.WishlistItems);
        Assert.Same(item, family.WishlistItems[0]);
        Assert.Equal(family.Id, item.FamilyId);
        Assert.Same(family, item.Family);
        Assert.Equal(work.Id, item.BookWorkId);
        Assert.Same(work, item.BookWork);
        Assert.Equal(edition.Id, item.BookEditionId);
        Assert.Same(edition, item.BookEdition);
        Assert.Equal("Charlotte's Web", item.Title);
        Assert.Equal("E. B. White", item.Author);
    }

    [Fact]
    public void Family_add_wishlist_item_uses_fuzzy_item_when_no_work_or_edition_is_supplied()
    {
        var family = Family.Create("The Yans");

        var item = family.AddWishlistItem("  The Lion, the Witch and the Wardrobe  ", "  C. S. Lewis  ");

        Assert.Single(family.WishlistItems);
        Assert.Equal("The Lion, the Witch and the Wardrobe", item.Title);
        Assert.Equal("C. S. Lewis", item.Author);
        Assert.Null(item.BookWorkId);
        Assert.Null(item.BookWork);
        Assert.Null(item.BookEditionId);
        Assert.Null(item.BookEdition);
    }

    [Fact]
    public void Family_add_wishlist_item_throws_when_edition_belongs_to_a_different_work()
    {
        var family = Family.Create("The Yans");
        var work = BookWork.Create("Charlotte's Web");
        var otherWork = BookWork.Create("Matilda");
        var edition = otherWork.AddEdition(isbn: "978-0-06-112495-2");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            family.AddWishlistItem("Charlotte's Web", bookWork: work, bookEdition: edition));

        Assert.Equal("Edition must belong to the same work as the wishlist item.", exception.Message);
        Assert.Empty(family.WishlistItems);
    }

    [Fact]
    public void Family_add_wishlist_item_throws_when_edition_is_not_attached_to_a_work()
    {
        var family = Family.Create("The Yans");
        var edition = new BookEdition();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            family.AddWishlistItem("Charlotte's Web", bookEdition: edition));

        Assert.Equal("Edition must belong to a work before it can be added to the wishlist.", exception.Message);
        Assert.Empty(family.WishlistItems);
    }

    [Fact]
    public void Family_add_wishlist_item_throws_when_an_unattached_edition_is_supplied_with_a_work()
    {
        var family = Family.Create("The Yans");
        var work = BookWork.Create("Charlotte's Web");
        var edition = new BookEdition();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            family.AddWishlistItem("Charlotte's Web", bookWork: work, bookEdition: edition));

        Assert.Equal("Edition must belong to a work before it can be added to the wishlist.", exception.Message);
        Assert.Empty(family.WishlistItems);
    }
}
