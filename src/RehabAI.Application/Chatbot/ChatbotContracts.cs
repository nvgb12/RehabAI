namespace RehabAI.Application.Chatbot;

public sealed record ChatRequest(Guid? UserId, string? GuestSessionId, Guid? SessionId, string Message);
public sealed record ChatResponse(Guid SessionId, string Message);

public interface IChatbotService
{
    Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken = default);
}

public interface IAiChatClient
{
    Task<string> CompleteAsync(IReadOnlyCollection<(string Role, string Content)> messages, CancellationToken cancellationToken = default);
}
