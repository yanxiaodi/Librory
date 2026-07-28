using Librory.Application.Scanning;
using Librory.Domain.Models;
using Librory.Infrastructure.Persistence;
using Librory.Infrastructure.Scanning;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Librory.Api.Tests;

public sealed class ScanCleanupTests
{
    [Fact]
    public async Task Cleanup_deletes_expired_scan_sessions_and_their_temp_files()
    {
        await using var factory = await ApiFactory.CreateAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });

        var bootstrapResponse = await client.PostAsync("/dev/bootstrap", content: null);
        Assert.True(bootstrapResponse.IsSuccessStatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibroryDbContext>();
        var cleanup = scope.ServiceProvider.GetRequiredService<IScanSessionCleanupService>();
        var storageRoot = ScanStorageTestPaths.GetTemporaryRoot(scope.ServiceProvider);

        var family = await db.Families.SingleAsync();
        var tempFilePath = Path.Combine(storageRoot, $"{Guid.NewGuid():N}.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(tempFilePath)!);
        await File.WriteAllBytesAsync(tempFilePath, [0x01, 0x02, 0x03]);

        var session = ScanSession.Create(family, tempFilePath, TimeSpan.FromMinutes(1));
        db.ScanSessions.Add(session);
        await db.SaveChangesAsync();

        db.Entry(session).Property(x => x.ExpiresAt).CurrentValue = DateTimeOffset.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        var deleted = await cleanup.DeleteExpiredTemporaryScanDataAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Equal(1, deleted);
        Assert.Empty(await db.ScanSessions.ToListAsync());
        Assert.False(File.Exists(tempFilePath));
    }

    [Fact]
    public async Task Cleanup_hosted_service_is_registered_in_the_api_container()
    {
        await using var factory = await ApiFactory.CreateAsync();

        using var scope = factory.Services.CreateScope();
        var hostedServices = scope.ServiceProvider.GetServices<IHostedService>().OfType<ScanCleanupHostedService>().ToList();

        Assert.Single(hostedServices);
    }
}
