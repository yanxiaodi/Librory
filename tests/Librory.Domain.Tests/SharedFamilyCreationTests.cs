using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public class SharedFamilyCreationTests
{
    [Fact]
    public void CreateSharedFamily_trims_name_and_promotes_admin()
    {
        var family = Family.CreateSharedFamily("  The Yans  ", "  Alice  ", PreferredLanguage.Chinese);

        Assert.Equal("The Yans", family.Name);
        Assert.Single(family.Members);

        var admin = family.Members[0];
        Assert.Equal("Alice", admin.DisplayName);
        Assert.Equal(MemberRole.Admin, admin.Role);
        Assert.Equal(PreferredLanguage.Chinese, admin.PreferredLanguage);
        Assert.Equal(family.Id, admin.FamilyId);
    }

    [Fact]
    public void CreateSharedFamily_uses_default_language_when_not_specified()
    {
        var family = Family.CreateSharedFamily("The Yans", "Alice");

        var admin = family.Members[0];

        Assert.Equal(PreferredLanguage.English, admin.PreferredLanguage);
    }
}
