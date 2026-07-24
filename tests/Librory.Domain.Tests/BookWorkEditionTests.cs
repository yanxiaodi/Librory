using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public class BookWorkEditionTests
{
    [Fact]
    public void BookWork_create_trims_title_and_optional_author()
    {
        var work = BookWork.Create("  Charlotte's Web  ", "  E. B. White  ");

        Assert.Equal("Charlotte's Web", work.CanonicalTitle);
        Assert.Equal("E. B. White", work.CanonicalAuthor);
        Assert.Empty(work.Editions);
    }

    [Fact]
    public void BookWork_create_throws_when_title_is_blank()
    {
        Assert.Throws<ArgumentException>(() => BookWork.Create(" "));
    }

    [Fact]
    public void BookWork_add_edition_registers_the_edition()
    {
        var work = BookWork.Create("Charlotte's Web", "E. B. White");

        var edition = work.AddEdition(
            isbn: " 978-0-06-112495-2 ",
            format: "  Hardcover ",
            publicationYear: 2006);

        Assert.Equal(work.Id, edition.BookWorkId);
        Assert.Same(work, edition.BookWork);
        Assert.Single(work.Editions);
        Assert.Same(edition, work.Editions[0]);
        Assert.Equal("978-0-06-112495-2", edition.Isbn);
        Assert.Equal("Hardcover", edition.Format);
        Assert.Equal(2006, edition.PublicationYear);
    }

    [Fact]
    public void BookWork_add_edition_uses_null_for_blank_optional_fields()
    {
        var work = BookWork.Create("Charlotte's Web");

        var edition = work.AddEdition(" ", " ", null);

        Assert.Null(edition.Isbn);
        Assert.Null(edition.Format);
        Assert.Null(edition.PublicationYear);
    }

    [Fact]
    public void BookEdition_cannot_be_reassigned_to_a_different_work()
    {
        var firstWork = BookWork.Create("Charlotte's Web");
        var secondWork = BookWork.Create("Animal Farm");
        var edition = firstWork.AddEdition(isbn: "978-0-06-112495-2");

        var exception = Assert.Throws<InvalidOperationException>(() => edition.AssignToWork(secondWork));

        Assert.Equal("Edition already belongs to a different work.", exception.Message);
        Assert.Equal(firstWork.Id, edition.BookWorkId);
        Assert.Same(firstWork, edition.BookWork);
        Assert.Single(firstWork.Editions);
        Assert.Empty(secondWork.Editions);
    }

    [Fact]
    public void AssignToWork_is_idempotent_for_same_work()
    {
        var work = BookWork.Create("Charlotte's Web");
        var edition = work.AddEdition(isbn: "978-0-06-112495-2");

        edition.AssignToWork(work);

        Assert.Single(work.Editions);
        Assert.Same(edition, work.Editions[0]);
    }
}
