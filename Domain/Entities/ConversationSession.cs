using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Domain.Entities;

public class ConversationSession
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public bool IsActive { get; private set; }
    private readonly List<Message> _messages = new();
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    private ConversationSession() { }

    public ConversationSession(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId cannot be empty.");

        Id = Guid.NewGuid();
        UserId = userId;
        StartedAt = DateTime.UtcNow;
        IsActive = true;
    }

    public Message AddMessage(string senderRole, string content, List<ValueObjects.Citation>? citations = null)
    {
        if (!IsActive)
            throw new DomainException("Cannot add message to an inactive session.");

        var message = new Message(Id, senderRole, content, citations);
        _messages.Add(message);
        return message;
    }

    public Message AddUserMessage(string content)
    {
        return AddMessage("User", content);
    }

    public Message AddAssistantMessage(string content, List<ValueObjects.Citation>? citations = null)
    {
        return AddMessage("Assistant", content, citations);
    }

    public void CloseSession()
    {
        IsActive = false;
    }
}
