using Librory.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Librory.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly PostgresTestDatabase _database;
    private readonly string _connectionString;

    public ApiFactory()
    {
        _database = PostgresTestDatabase.CreateAsync().GetAwaiter().GetResult();
        _connectionString = _database.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LibroryDb"] = _connectionString,
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();

            services.RemoveAll<DbContextOptions<LibroryDbContext>>();
            services.RemoveAll<LibroryDbContext>();
            services.AddDbContext<LibroryDbContext>((_, options) =>
            {
                options.UseNpgsql(_connectionString);
            });

            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();
            db.Database.Migrate();
        });
    }

    public new void Dispose()
    {
        base.Dispose();
        _database.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
