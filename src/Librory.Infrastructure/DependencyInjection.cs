using Librory.Application.Scanning;
using Librory.Application.Identity;
using Librory.Infrastructure.Identity;
using Librory.Infrastructure.Persistence;
using Librory.Infrastructure.Scanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Librory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLibroryInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<LibroryDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = LibroryDbConnectionStringResolver.Resolve(configuration);

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IScanSessionService, ScanSessionService>();
        services.AddScoped<IExternalLoginService, ExternalLoginService>();

        return services;
    }
}
