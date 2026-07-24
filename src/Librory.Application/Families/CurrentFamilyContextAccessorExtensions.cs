namespace Librory.Application.Families;

public static class CurrentFamilyContextAccessorExtensions
{
    public static CurrentFamilyContext RequireCurrent(this ICurrentFamilyContextAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);

        return accessor.Current
            ?? throw new InvalidOperationException("Current family context is not available.");
    }
}
