namespace Librory.Domain.Models;

public sealed class Member
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid FamilyId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public PreferredLanguage PreferredLanguage { get; set; } = PreferredLanguage.English;
}
