namespace Librory.Application.Recognition;

public sealed record RecognizedTextBlock(
    string Text,
    decimal Confidence,
    int Left,
    int Top,
    int Right,
    int Bottom,
    bool IsVertical);
