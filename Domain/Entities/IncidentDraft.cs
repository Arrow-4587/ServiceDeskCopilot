using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Exceptions;
using ServiceDesk.Domain.Services;

namespace ServiceDesk.Domain.Entities;

public class IncidentDraft
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public IncidentImpact Impact { get; private set; }
    public IncidentUrgency Urgency { get; private set; }
    public IncidentPriority ComputedPriority { get; private set; }
    public string SystemStatusEvidence { get; private set; } = string.Empty;
    public ApprovalStatus ApprovalStatus { get; private set; }
    public IncidentStatus Status { get; private set; }
    public string? SubmittedIncidentId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public Guid? AssignedAnalystId { get; private set; }
    public string? ResolutionFeedback { get; private set; }

    private IncidentDraft() { }

    public IncidentDraft(
        Guid userId,
        string title,
        string description,
        string category,
        IncidentImpact impact,
        IncidentUrgency urgency,
        string systemStatusEvidence = "")
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId cannot be empty.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Incident draft title cannot be empty.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Incident draft description cannot be empty.");

        Id = Guid.NewGuid();
        UserId = userId;
        Title = title.Trim();
        Description = description.Trim();
        Category = string.IsNullOrWhiteSpace(category) ? "General IT" : category.Trim();
        Impact = impact;
        Urgency = urgency;
        ComputedPriority = DeterministicPriorityCalculator.Calculate(impact, urgency);
        SystemStatusEvidence = systemStatusEvidence ?? string.Empty;
        ApprovalStatus = ApprovalStatus.Pending;
        Status = IncidentStatus.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDraft(string title, string description, IncidentImpact impact, IncidentUrgency urgency, string category)
    {
        if (ApprovalStatus != ApprovalStatus.Pending)
            throw new DomainException("Cannot update an incident draft that has already been decided.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Incident draft title cannot be empty.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Incident draft description cannot be empty.");

        Title = title.Trim();
        Description = description.Trim();
        Category = string.IsNullOrWhiteSpace(category) ? Category : category.Trim();
        Impact = impact;
        Urgency = urgency;
        ComputedPriority = DeterministicPriorityCalculator.Calculate(impact, urgency);
    }

    public void ApproveByAuthenticatedUser()
    {
        if (ApprovalStatus != ApprovalStatus.Pending)
            throw new DomainException("Draft has already been decided.");

        ApprovalStatus = ApprovalStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
    }

    public void RejectByAuthenticatedUser()
    {
        if (ApprovalStatus != ApprovalStatus.Pending)
            throw new DomainException("Draft has already been decided.");

        ApprovalStatus = ApprovalStatus.Rejected;
    }

    public void MarkSubmitted(string ticketId)
    {
        if (ApprovalStatus != ApprovalStatus.Approved)
            throw new DomainException("Cannot submit ticket without explicit user approval.");

        if (string.IsNullOrWhiteSpace(ticketId))
            throw new DomainException("Submitted Ticket ID cannot be empty.");

        SubmittedIncidentId = ticketId;
        Status = IncidentStatus.Submitted;
    }

    public void AssignTo(Guid analystId)
    {
        if (analystId == Guid.Empty)
            throw new DomainException("Analyst ID cannot be empty.");

        AssignedAnalystId = analystId;
    }

    public void ProvideFeedback(string feedback)
    {
        if (string.IsNullOrWhiteSpace(feedback))
            throw new DomainException("Feedback cannot be empty.");

        ResolutionFeedback = feedback.Trim();
    }
}
