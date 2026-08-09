using Librory.Application.Scanning;
using Librory.Application.Identity;
using Librory.Application.Metadata;
using Librory.Application.Recognition;
using Librory.Infrastructure.Identity;
using Librory.Infrastructure.Metadata.GoogleBooks;
using Librory.Infrastructure.Metadata;
using Librory.Infrastructure.Recognition;
using Librory.Infrastructure.Persistence;
using Librory.Infrastructure.Scanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Librory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLibroryInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<LibroryDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = LibroryDbConnectionStringResolver.Resolve(configuration);

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IScanSessionService, ScanSessionService>();
        services.AddScoped<IScanSessionCleanupService, ExpiredScanSessionCleanupService>();
        services.AddSingleton<LocalScanPhotoStorage>();
        services.AddSingleton<IScanPhotoStorage>(serviceProvider => serviceProvider.GetRequiredService<LocalScanPhotoStorage>());
        services.AddOptions<RecognitionOptions>()
            .BindConfiguration("Recognition");
        services.AddSingleton<BookTitleCandidateRanker>();
        services.AddHttpClient<IOcrTextExtractionService, DocumentIntelligenceTextExtractionService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        });
        services.AddHttpClient<IVisionFallbackService, AzureOpenAiVisionFallbackService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IBookRecognitionPipeline, BookRecognitionPipeline>();
        services.AddScoped<IBookRecognitionJobService, BookRecognitionJobService>();
        services.AddScoped<BookRecognitionJobProcessor>();
        services.AddOptions<GoogleBooksOptions>()
            .BindConfiguration(GoogleBooksOptions.SectionName);
        services.AddHttpClient<IBookMetadataSearchService, GoogleBooksMetadataSearchService>(client =>
        {
            client.BaseAddress = new Uri("https://www.googleapis.com/books/v1/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddScoped<IBookMetadataImportService, BookMetadataImportService>();
        services.AddHostedService<ScanCleanupHostedService>();
        services.AddHostedService<BookRecognitionJobProcessorHostedService>();
        services.AddScoped<IExternalLoginService, ExternalLoginService>();

        return services;
    }
}
