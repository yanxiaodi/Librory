namespace Librory.Application.Scanning;

public sealed class ScanSessionOptions
{
    public const int DefaultPhotoRetentionDays = 7;

    public int PhotoRetentionDays { get; set; } = DefaultPhotoRetentionDays;
}
