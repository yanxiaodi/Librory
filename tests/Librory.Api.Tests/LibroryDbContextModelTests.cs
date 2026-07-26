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

    [Fact]
    public void Model_enforces_family_and_member_uniqueness()
    {
        var options = new DbContextOptionsBuilder<LibroryDbContext>()
            .UseInMemoryDatabase(nameof(Model_enforces_family_and_member_uniqueness))
            .Options;

        using var db = new LibroryDbContext(options);

        var familyType = db.Model.FindEntityType(typeof(Family));
        var memberType = db.Model.FindEntityType(typeof(Member));

        Assert.NotNull(familyType);
        Assert.NotNull(memberType);

        Assert.Contains(familyType!.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(["Name"]));

        Assert.Contains(memberType!.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(["FamilyId", "DisplayName"]));
    }

    [Fact]
    public void Model_persists_member_external_identities()
    {
        var options = new DbContextOptionsBuilder<LibroryDbContext>()
            .UseInMemoryDatabase(nameof(Model_persists_member_external_identities))
            .Options;

        using var db = new LibroryDbContext(options);

        var externalIdentityType = db.Model.GetEntityTypes()
            .Single(entity => entity.GetTableName() == "member_external_identities");

        Assert.Contains(externalIdentityType.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(["Provider", "ProviderSubject"]));
    }
}
