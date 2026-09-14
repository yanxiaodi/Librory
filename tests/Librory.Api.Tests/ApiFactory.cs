using Librory.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Librory.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly PostgresTestDatabase _database;
    private readonly string _connectionString;
    private readonly DbCommandInterceptor? _databaseInterceptor;

    private ApiFactory(PostgresTestDatabase database, DbCommandInterceptor? databaseInterceptor)
    {
        _database = database;
        _connectionString = database.ConnectionString;
        _databaseInterceptor = databaseInterceptor;
    }

    public static async Task<ApiFactory> CreateAsync(DbCommandInterceptor? databaseInterceptor = null)
    {
        var database = await PostgresTestDatabase.CreateAsync();
        return new ApiFactory(database, databaseInterceptor);
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
                if (_databaseInterceptor is not null)
                {
                    options.AddInterceptors(_databaseInterceptor);
                }
            });
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

}
