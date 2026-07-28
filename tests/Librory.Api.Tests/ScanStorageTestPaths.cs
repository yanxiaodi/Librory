using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Librory.Api.Tests;

internal static class ScanStorageTestPaths
{
    public static string GetTemporaryRoot(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
        var configuredRoot = configuration["ScanStorage:TemporaryRoot"]?.Trim();

        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            configuredRoot = "scan-uploads";
        }

        return Path.IsPathRooted(configuredRoot)
            ? Path.GetFullPath(configuredRoot)
            : Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, configuredRoot));
    }
}
