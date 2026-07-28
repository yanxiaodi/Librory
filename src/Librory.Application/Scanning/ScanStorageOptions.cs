namespace Librory.Application.Scanning;

public sealed class ScanStorageOptions
{
    public const string DefaultTemporaryRoot = "scan-uploads";

    public string TemporaryRoot { get; set; } = DefaultTemporaryRoot;
}
