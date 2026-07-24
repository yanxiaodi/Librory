using Librory.Application.Families;
using Microsoft.Extensions.DependencyInjection;

namespace Librory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddLibroryApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<ICurrentFamilyContextAccessor, CurrentFamilyContextAccessor>();
        return services;
    }
}
