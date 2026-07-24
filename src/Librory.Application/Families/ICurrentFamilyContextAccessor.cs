namespace Librory.Application.Families;

public interface ICurrentFamilyContextAccessor
{
    CurrentFamilyContext? Current { get; set; }
}
