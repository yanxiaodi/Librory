namespace Librory.Application.Recognition;

public sealed class RecognitionOptions
{
    public DocumentIntelligenceOptions DocumentIntelligence { get; set; } = new();

    public AzureOpenAiOptions AzureOpenAI { get; set; } = new();
}

public sealed class DocumentIntelligenceOptions
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
