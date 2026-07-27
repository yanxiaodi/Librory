using Librory.Application.Scanning;

namespace Librory.Infrastructure.Scanning;

public sealed class LocalScanPhotoStorage : IScanPhotoStorage
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "image/heic",
        "image/heif",
    };

    private static readonly IReadOnlyDictionary<string, string> ContentTypeExtensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/jpg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/heic"] = ".heic",
        ["image/heif"] = ".heif",
    };

    private readonly string _rootDirectory;

    public LocalScanPhotoStorage()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "Librory", "scan-uploads");
    }

    public async Task<string> StoreTemporaryAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (contentType is null || !SupportedContentTypes.Contains(contentType))
        {
            throw new ArgumentException("Uploaded shelf photo must be an image.", nameof(contentType));
        }

        if (content.CanSeek && content.Length > MaxUploadBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(content), "Uploaded shelf photo is too large.");
        }

        Directory.CreateDirectory(_rootDirectory);

        var extension = TryGetExtension(originalFileName, contentType);
        var filePath = Path.Combine(_rootDirectory, $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{extension}");

        await using var destination = File.Create(filePath);
        await content.CopyToAsync(destination, cancellationToken);

        return filePath;
    }

    public Task DeleteAsync(string shelfPhotoPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shelfPhotoPath);

        if (File.Exists(shelfPhotoPath))
        {
            File.Delete(shelfPhotoPath);
        }

        return Task.CompletedTask;
    }

    private static string TryGetExtension(string originalFileName, string contentType)
    {
        var originalExtension = Path.GetExtension(originalFileName);
        if (!string.IsNullOrWhiteSpace(originalExtension))
        {
            return originalExtension.Trim();
        }

        return ContentTypeExtensions.TryGetValue(contentType, out var mappedExtension)
            ? mappedExtension
            : ".jpg";
    }
}
