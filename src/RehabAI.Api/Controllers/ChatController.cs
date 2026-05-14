using Microsoft.AspNetCore.Mvc;
using RehabAI.Api.Contracts.Chat;
using RehabAI.Application.Chatbot;

namespace RehabAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController(IChatbotService chatbotService) : ControllerBase
{
    [HttpPost("send")]
    public async Task<IActionResult> Send(SendChatRequest request, CancellationToken cancellationToken)
    {
        var response = await chatbotService.SendAsync(new ChatRequest(request.UserId, request.GuestSessionId, request.SessionId, request.Message), cancellationToken);
        return Ok(response);
    }
}
