namespace Librory.Application.Recognition;

public sealed class RecognitionOptions
{
    public AzureVisionOptions AzureVision { get; set; } = new();

    public AzureOpenAiOptions AzureOpenAI { get; set; } = new();
}

public sealed class AzureVisionOptions
{
    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}

public sealed class AzureOpenAiOptions
{
    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string DeploymentName { get; set; } = string.Empty;
}
