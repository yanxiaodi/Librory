using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public class FamilyMemberPersistenceTests
{
    [Fact]
    public void Family_create_throws_when_name_is_blank()
    {
        Assert.Throws<ArgumentException>(() => Family.Create(" "));
    }

    [Fact]
    public void Family_create_trims_name_and_starts_empty()
    {
        var family = Family.Create("  The Yans  ");

        Assert.Equal("The Yans", family.Name);
        Assert.Empty(family.Members);
    }

    [Fact]
    public void Family_add_member_registers_family_role_and_language()
    {
        var family = Family.Create("The Yans");

        var member = family.AddMember("  Alice  ");

        Assert.Equal("Alice", member.DisplayName);
        Assert.Equal(MemberRole.Member, member.Role);
        Assert.Equal(PreferredLanguage.English, member.PreferredLanguage);
        Assert.Equal(family.Id, member.FamilyId);
        Assert.Same(family, member.Family);
        Assert.Single(family.Members);
        Assert.Same(member, family.Members[0]);
    }

    [Fact]
    public void Family_add_member_accepts_custom_role_and_language()
    {
        var family = Family.Create("The Yans");

        var admin = family.AddMember("Alice", MemberRole.Admin, PreferredLanguage.Chinese);

        Assert.Equal(MemberRole.Admin, admin.Role);
        Assert.Equal(PreferredLanguage.Chinese, admin.PreferredLanguage);
    }

    [Fact]
    public void Family_add_member_throws_when_display_name_is_blank()
    {
        var family = Family.Create("The Yans");

        Assert.Throws<ArgumentException>(() => family.AddMember(" "));
    }

    [Fact]
    public void AssignToFamily_throws_when_family_is_null()
    {
        var member = new Member();

        Assert.Throws<ArgumentNullException>(() => member.AssignToFamily(null!));
    }

    [Fact]
    public void AssignToFamily_is_idempotent_for_same_family()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");

        member.AssignToFamily(family);

        Assert.Single(family.Members);
        Assert.Same(member, family.Members[0]);
    }

    [Fact]
    public void Member_cannot_be_reassigned_to_a_different_family()
    {
        var firstFamily = Family.Create("First");
        var secondFamily = Family.Create("Second");

        var member = firstFamily.AddMember("Alice");

        var exception = Assert.Throws<InvalidOperationException>(() => member.AssignToFamily(secondFamily));

        Assert.Equal("Member already belongs to a different family.", exception.Message);
        Assert.Equal(firstFamily.Id, member.FamilyId);
        Assert.Same(firstFamily, member.Family);
        Assert.Single(firstFamily.Members);
        Assert.Empty(secondFamily.Members);
    }
}
