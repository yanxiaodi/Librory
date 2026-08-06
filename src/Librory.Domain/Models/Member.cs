namespace Librory.Domain.Models;

public sealed class Member
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid FamilyId { get; private set; }
    public Guid? UserAccountId { get; private set; }
    public string DisplayName { get; set; } = string.Empty;
    public MemberRole Role { get; set; } = MemberRole.Member;
    public PreferredLanguage PreferredLanguage { get; set; } = PreferredLanguage.English;
    public bool IsActive { get; private set; } = true;
    public UserAccount? UserAccount { get; private set; }
    public Family Family { get; private set; } = null!;

    public void LinkAccount(UserAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (UserAccountId is not null && UserAccountId != account.Id)
        {
            throw new InvalidOperationException("Member is already linked to a different account.");
        }

        UserAccountId = account.Id;
        UserAccount = account;
        account.RegisterMembership(this);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate()
    {
        IsActive = true;
    }

    public void AssignToFamily(Family family)
    {
        ArgumentNullException.ThrowIfNull(family);

        if (FamilyId != Guid.Empty && FamilyId != family.Id)
        {
            throw new InvalidOperationException("Member already belongs to a different family.");
        }

        FamilyId = family.Id;
        Family = family;
        family.RegisterMember(this);
    }
}
