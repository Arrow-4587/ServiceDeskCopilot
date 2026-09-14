using ServiceDesk.Domain.ValueObjects;

namespace ServiceDesk.Domain.Entities;

public class Message
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public string SenderRole { get; private set; } = string.Empty; // "User", "Assistant", "System"
    public string Content { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }
    public List<Citation> Citations { get; private set; } = new();

    private Message() { }

    public Message(Guid conversationId, string senderRole, string content, List<Citation>? citations = null)
    {
        Id = Guid.NewGuid();
        ConversationId = conversationId;
        SenderRole = senderRole ?? "User";
        Content = content ?? string.Empty;
        Timestamp = DateTime.UtcNow;
        if (citations != null)
        {
            Citations.AddRange(citations);
        }
    }
}
