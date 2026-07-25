using Librory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Librory.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<LibroryDbContext>>();
            services.RemoveAll<LibroryDbContext>();

            var inMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddSingleton(inMemoryProvider);
            services.AddDbContext<LibroryDbContext>((_, options) =>
            {
                options.UseInMemoryDatabase(nameof(LibroryDbContext));
                options.UseInternalServiceProvider(inMemoryProvider);
            });
        });
    }
}
