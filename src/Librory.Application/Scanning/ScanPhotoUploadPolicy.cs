namespace Librory.Application.Scanning;

public static class ScanPhotoUploadPolicy
{
    public const long MaxUploadBytes = 10 * 1024 * 1024;

    public static IReadOnlySet<string> AllowedImageContentTypes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "image/heic",
        "image/heif",
    };
}
