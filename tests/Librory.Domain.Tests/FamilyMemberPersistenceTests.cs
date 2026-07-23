using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public class FamilyMemberPersistenceTests
{
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
