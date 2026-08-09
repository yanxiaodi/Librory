using Microsoft.Extensions.AI;

namespace Librory.Infrastructure.Recognition;

/// <summary>
/// Creates the chat client used to run the vision candidate-extraction agent.
/// Kept as a seam so tests can substitute a fake chat client without a real Azure OpenAI resource.
/// </summary>
public interface IBookVisionChatClientFactory
{
    /// <returns>The chat client, or <see langword="null"/> when Agent Framework is not configured.</returns>
    IChatClient? CreateChatClient();
}
