using Librory.Application.Families;
using Librory.Domain.Models;
using Xunit;

namespace Librory.Application.Tests;

public class SharedFamilyCreatorTests
{
    [Fact]
    public void Create_builds_shared_family_with_admin_member()
    {
        var result = SharedFamilyCreator.Create(
            "  The Yans  ",
            "  Alice  ",
            PreferredLanguage.Chinese);

        Assert.Equal("The Yans", result.Family.Name);
        Assert.Single(result.Family.Members);
        Assert.Same(result.InitialMember, result.Family.Members[0]);
        Assert.Equal(MemberRole.Admin, result.InitialMember.Role);
        Assert.Equal("Alice", result.InitialMember.DisplayName);
        Assert.Equal(PreferredLanguage.Chinese, result.InitialMember.PreferredLanguage);
        Assert.Equal(result.Family.Id, result.InitialMember.FamilyId);
    }

    [Fact]
    public void Create_uses_default_preferred_language_when_not_specified()
    {
        var result = SharedFamilyCreator.Create("The Yans", "Alice");

        Assert.Equal(PreferredLanguage.English, result.InitialMember.PreferredLanguage);
    }

    [Fact]
    public void Create_throws_when_family_name_is_blank()
    {
        Assert.Throws<ArgumentException>(() => SharedFamilyCreator.Create(
            " ",
            "Alice"));
    }

    [Fact]
    public void Create_throws_when_admin_display_name_is_blank()
    {
        Assert.Throws<ArgumentException>(() => SharedFamilyCreator.Create(
            "The Yans",
            " "));
    }
}
