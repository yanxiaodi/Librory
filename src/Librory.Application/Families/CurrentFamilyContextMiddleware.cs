using Microsoft.AspNetCore.Http;

namespace Librory.Application.Families;

public sealed class CurrentFamilyContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        ICurrentFamilyContextAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(accessor);

        accessor.Current = CurrentFamilyContextResolver.TryResolve(context.User, out var current)
            ? current
            : null;

        await next(context);
    }
}
