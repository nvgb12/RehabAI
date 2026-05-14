using RehabAI.Domain.Enums;

namespace RehabAI.Domain.Entities;

public class ChatSession : BaseEntity
{
    public Guid? UserId { get; set; }
    public string? GuestSessionId { get; set; }
    public string? LinkedFromGuestSessionId { get; set; }
    public DateTimeOffset? LinkedAt { get; set; }
    public string? Title { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public class ChatMessage : BaseEntity
{
    public Guid ChatSessionId { get; set; }
    public ChatMessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
}

public class EmailLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}

public class AuditLog : BaseEntity
{
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public string? MetadataJson { get; set; }
}

public class SystemSetting : BaseEntity
{
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string ValueType { get; set; } = "string";
    public string? Description { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}

public class AiUsageDaily : BaseEntity
{
    public Guid? UserId { get; set; }
    public string? GuestSessionId { get; set; }
    public DateOnly UsageDate { get; set; }
    public int MessageCount { get; set; }
    public int TokenCount { get; set; }
    public decimal? CostAmount { get; set; }
}
