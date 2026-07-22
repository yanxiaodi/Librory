using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public class MemberRoleTests
{
    [Fact]
    public void MemberRole_includes_admin_and_member()
    {
        Assert.Contains(MemberRole.Admin, Enum.GetValues<MemberRole>());
        Assert.Contains(MemberRole.Member, Enum.GetValues<MemberRole>());
    }
}
