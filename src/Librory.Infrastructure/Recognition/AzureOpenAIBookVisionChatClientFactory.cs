using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace Librory.Infrastructure.Recognition;

public sealed class AzureOpenAIBookVisionChatClientFactory : IBookVisionChatClientFactory
{
    private readonly AgentFrameworkOptions _options;

    public AzureOpenAIBookVisionChatClientFactory(IOptions<AgentFrameworkOptions> options)
    {
        _options = options.Value;
    }

    public IChatClient? CreateChatClient()
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint)
            || string.IsNullOrWhiteSpace(_options.ApiKey)
            || string.IsNullOrWhiteSpace(_options.DeploymentName))
        {
            return null;
        }

        // The v1 endpoint style (…/openai/v1) works for both classic Azure OpenAI resources and
        // Foundry resources; it requires the plain OpenAI SDK client rather than AzureOpenAIClient,
        // which targets the older …/openai/deployments/{name}/... URL shape and 404s against v1 endpoints.
        var chatClient = new ChatClient(
            _options.DeploymentName,
            new ApiKeyCredential(_options.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(_options.Endpoint) });

        return chatClient.AsIChatClient();
    }
}
