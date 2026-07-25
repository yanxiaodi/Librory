using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Librory.Infrastructure.Persistence;

public sealed class DesignTimeLibroryDbContextFactory : IDesignTimeDbContextFactory<LibroryDbContext>
{
    public LibroryDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<DesignTimeLibroryDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = LibroryDbConnectionStringResolver.Resolve(configuration);

        var optionsBuilder = new DbContextOptionsBuilder<LibroryDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new LibroryDbContext(optionsBuilder.Options);
    }
}
