namespace RehabAI.Api.Contracts.Chat;

public sealed record SendChatRequest(Guid? UserId, string? GuestSessionId, Guid? SessionId, string Message);
