namespace Librory.Application.Scanning;

public sealed record CorrectionRequest(
    string? RescannedPhotoPath,
    string? ManualTitle);
