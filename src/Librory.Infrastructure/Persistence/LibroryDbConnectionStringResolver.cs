using Microsoft.Extensions.Configuration;

namespace Librory.Infrastructure.Persistence;

internal static class LibroryDbConnectionStringResolver
{
    private const string ConnectionStringName = "LibroryDb";
    private const string EnvironmentVariableName = "LIBRORY_DATABASE_URL";
    private const string LocalFallback =
        "Host=localhost;Port=5432;Database=librory;Username=postgres;Password=postgres";

    public static string Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var environmentVariable = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(environmentVariable))
        {
            return environmentVariable;
        }

        return LocalFallback;
    }
}
