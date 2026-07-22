using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Librory.Infrastructure.Persistence;

public static class DatabaseInitializationExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();

        await context.Database.MigrateAsync();
        await DbInitializer.SeedAsync(context);
    }
}
