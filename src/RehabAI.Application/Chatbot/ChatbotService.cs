namespace RehabAI.Application.Chatbot;

public class ChatbotService(IAiChatClient aiChatClient) : IChatbotService
{
    public async Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var sessionId = request.SessionId ?? Guid.NewGuid();
        var messages = new List<(string Role, string Content)>
        {
            ("system", "You are Rehab AI. Provide general rehabilitation and hospital service guidance only. Do not provide final diagnosis or emergency instructions."),
            ("user", request.Message)
        };

        var reply = await aiChatClient.CompleteAsync(messages, cancellationToken);
        return new ChatResponse(sessionId, reply);
    }
}
