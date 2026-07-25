using Microsoft.Extensions.Configuration;

namespace Librory.Infrastructure.Persistence;

internal static class LibroryDbConnectionStringResolver
{
    private const string ConnectionStringName = "LibroryDb";
    private const string EnvironmentVariableName = "LIBRORY_DATABASE_URL";

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

        throw new InvalidOperationException(
            $"Missing database connection string. Configure 'ConnectionStrings:{ConnectionStringName}' or '{EnvironmentVariableName}'.");
    }
}
