using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Librory.Infrastructure.Persistence;

public sealed class DesignTimeLibroryDbContextFactory : IDesignTimeDbContextFactory<LibroryDbContext>
{
    public LibroryDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = LibroryDbConnectionStringResolver.Resolve(configuration);

        var optionsBuilder = new DbContextOptionsBuilder<LibroryDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new LibroryDbContext(optionsBuilder.Options);
    }
}
