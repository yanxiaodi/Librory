using Librory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Librory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLibroryInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<LibroryDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(LibroryDbContext).Assembly.FullName)));

        return services;
    }
}
