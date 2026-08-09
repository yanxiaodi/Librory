namespace Librory.Infrastructure.Recognition;

public sealed class AgentFrameworkOptions
{
    public const string SectionName = "AgentFramework:AzureOpenAI";

    public string? Endpoint { get; set; }

    public string? ApiKey { get; set; }

    public string? DeploymentName { get; set; }
}