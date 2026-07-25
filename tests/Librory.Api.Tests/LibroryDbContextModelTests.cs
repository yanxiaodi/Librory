using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Librory.Api.Tests;

public sealed class LibroryDbContextModelTests
{
    [Fact]
    public void Model_uses_expected_schema_and_tables()
    {
        var options = new DbContextOptionsBuilder<LibroryDbContext>()
            .UseInMemoryDatabase(nameof(LibroryDbContextModelTests))
            .Options;

        using var db = new LibroryDbContext(options);

        Assert.Equal("librory", db.Model.GetDefaultSchema());
        Assert.Equal("families", db.Model.FindEntityType(typeof(Family))!.GetTableName());
        Assert.Equal("members", db.Model.FindEntityType(typeof(Member))!.GetTableName());
        Assert.Equal("book_works", db.Model.FindEntityType(typeof(BookWork))!.GetTableName());
        Assert.Equal("book_editions", db.Model.FindEntityType(typeof(BookEdition))!.GetTableName());
        Assert.Equal("book_copies", db.Model.FindEntityType(typeof(BookCopy))!.GetTableName());
        Assert.Equal("wishlist_items", db.Model.FindEntityType(typeof(WishlistItem))!.GetTableName());
    }
}
