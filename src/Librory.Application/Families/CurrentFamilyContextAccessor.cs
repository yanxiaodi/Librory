namespace Librory.Application.Families;

public sealed class CurrentFamilyContextAccessor : ICurrentFamilyContextAccessor
{
    public CurrentFamilyContext? Current { get; set; }
}
