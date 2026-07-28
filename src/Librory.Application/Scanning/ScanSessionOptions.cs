namespace Librory.Application.Scanning;

public sealed class ScanSessionOptions
{
    public const int DefaultPhotoRetentionDays = 7;
    public const int DefaultCleanupIntervalHours = 24;

    public int PhotoRetentionDays { get; set; } = DefaultPhotoRetentionDays;
    public int CleanupIntervalHours { get; set; } = DefaultCleanupIntervalHours;
}
