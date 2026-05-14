using RehabAI.Application.Chatbot;

namespace RehabAI.Infrastructure.Ai;

public class PlaceholderAiChatClient : IAiChatClient
{
    public Task<string> CompleteAsync(IReadOnlyCollection<(string Role, string Content)> messages, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Rehab AI chatbot placeholder. Replace with OpenAI or Azure OpenAI implementation.");
    }
}
