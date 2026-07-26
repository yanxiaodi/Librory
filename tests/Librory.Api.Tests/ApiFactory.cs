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

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly PostgresTestDatabase _database;
    private readonly string _connectionString;

    private ApiFactory(PostgresTestDatabase database)
    {
        _database = database;
        _connectionString = database.ConnectionString;
    }

    public static async Task<ApiFactory> CreateAsync()
    {
        var database = await PostgresTestDatabase.CreateAsync();
        return new ApiFactory(database);
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
                ["Authentication:Google:ClientId"] = "google-client-id",
                ["Authentication:Google:ClientSecret"] = "google-client-secret",
                ["Authentication:Microsoft:ClientId"] = "microsoft-client-id",
                ["Authentication:Microsoft:ClientSecret"] = "microsoft-client-secret",
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

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();
            db.Database.Migrate();
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

}
