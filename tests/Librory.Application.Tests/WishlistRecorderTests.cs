using Librory.Application.Wishlist;
using Librory.Domain.Models;
using Xunit;

namespace Librory.Application.Tests;

public class WishlistRecorderTests
{
    [Fact]
    public void Record_creates_a_wishlist_item_on_the_current_family()
    {
        var family = Family.Create("The Yans");
        var work = BookWork.Create("Charlotte's Web", "E. B. White");
        var edition = work.AddEdition(isbn: "978-0-06-112495-2", format: "Hardcover", publicationYear: 2006);
        var request = new WishlistItemRequest(
            "  Charlotte's Web  ",
            "  E. B. White  ",
            work,
            edition);

        var item = WishlistRecorder.Record(family, request);

        Assert.Single(family.WishlistItems);
        Assert.Same(item, family.WishlistItems[0]);
        Assert.Equal("Charlotte's Web", item.Title);
        Assert.Equal("E. B. White", item.Author);
        Assert.Equal(work.Id, item.BookWorkId);
        Assert.Equal(edition.Id, item.BookEditionId);
    }

    [Fact]
    public void Request_normalizes_whitespace_fields()
    {
        var request = new WishlistItemRequest("  The Borrowers  ", "   ");

        Assert.Equal("The Borrowers", request.Title);
        Assert.Null(request.Author);
    }

    [Fact]
    public void Dto_factory_preserves_work_and_edition_references()
    {
        var family = Family.Create("The Yans");
        var work = BookWork.Create("Charlotte's Web", "E. B. White");
        var edition = work.AddEdition(isbn: "978-0-06-112495-2");
        var item = family.AddWishlistItem("Charlotte's Web", "E. B. White", work, edition);

        var dto = WishlistItemDtoFactory.Create(item);

        Assert.Equal(item.Id, dto.WishlistItemId);
        Assert.Equal(work.Id, dto.BookWorkId);
        Assert.Equal(edition.Id, dto.BookEditionId);
        Assert.Equal("Charlotte's Web", dto.Title);
        Assert.Equal("E. B. White", dto.Author);
    }
}
