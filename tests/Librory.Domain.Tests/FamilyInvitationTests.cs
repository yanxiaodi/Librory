using Librory.Domain.Models;
using Xunit;

namespace Librory.Domain.Tests;

public sealed class FamilyInvitationTests
{
    [Fact]
    public void Placeholder_member_can_be_linked_to_one_account()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");
        var account = UserAccount.Create("alice@example.com");

        member.LinkAccount(account);

        Assert.Equal(account.Id, member.UserAccountId);
        Assert.Same(account, member.UserAccount);
        Assert.Contains(member, account.Memberships);
    }

    [Fact]
    public void Member_cannot_be_linked_to_a_different_account()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");
        var firstAccount = UserAccount.Create("alice@example.com");
        var secondAccount = UserAccount.Create("other@example.com");

        member.LinkAccount(firstAccount);

        var exception = Assert.Throws<InvalidOperationException>(() => member.LinkAccount(secondAccount));

        Assert.Equal("Member is already linked to a different account.", exception.Message);
    }

    [Fact]
    public void Deactivated_member_can_be_reactivated_without_changing_identity()
    {
        var family = Family.Create("The Yans");
        var member = family.AddMember("Alice");
        var memberId = member.Id;

        member.Deactivate();

        Assert.False(member.IsActive);

        member.Reactivate();

        Assert.True(member.IsActive);
        Assert.Equal(memberId, member.Id);
    }

    [Fact]
    public void Invitation_can_transition_from_pending_to_accepted_once()
    {
        var family = Family.Create("The Yans");
        var inviter = family.AddMember("Parent", MemberRole.Admin);
        var account = UserAccount.Create("alice@example.com");
        var invitation = FamilyInvitation.Create(
            family.Id,
            "alice@example.com",
            "token-hash",
            inviter.Id,
            DateTimeOffset.UtcNow.AddDays(7));

        invitation.Accept(account.Id, DateTimeOffset.UtcNow);

        Assert.Equal(FamilyInvitationStatus.Accepted, invitation.Status);
        Assert.Equal(account.Id, invitation.AcceptedAccountId);
        Assert.Throws<InvalidOperationException>(() => invitation.Accept(account.Id, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Invitation_can_be_revoked_or_superseded_only_while_pending()
    {
        var family = Family.Create("The Yans");
        var inviter = family.AddMember("Parent", MemberRole.Admin);
        var invitation = FamilyInvitation.Create(
            family.Id,
            "alice@example.com",
            "token-hash",
            inviter.Id,
            DateTimeOffset.UtcNow.AddDays(7));

        invitation.Revoke(inviter.Id, DateTimeOffset.UtcNow);

        Assert.Equal(FamilyInvitationStatus.Revoked, invitation.Status);
        Assert.Throws<InvalidOperationException>(() => invitation.Supersede(Guid.NewGuid()));
    }
}
