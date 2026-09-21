using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.DTOs.Dashboard;

public sealed class AnalystDashboardMetrics
{
    public int TriageQueueCount { get; init; }
    public int PendingApprovalCount { get; init; }
    public int EndUserSessionCount { get; init; }
    public int KnowledgeBaseCount { get; init; }

    public static AnalystDashboardMetrics From(
        IEnumerable<IncidentDraft> tickets,
        IEnumerable<ConversationSession> sessions,
        IEnumerable<Guid> relevantUserIds,
        int knowledgeBaseCount)
    {
        var relevantIds = new HashSet<Guid>(relevantUserIds);
        var ticketList = tickets ?? Enumerable.Empty<IncidentDraft>();
        var sessionList = sessions ?? Enumerable.Empty<ConversationSession>();

        return new AnalystDashboardMetrics
        {
            TriageQueueCount = ticketList.Count(t => relevantIds.Contains(t.UserId)),
            PendingApprovalCount = ticketList.Count(t => relevantIds.Contains(t.UserId) && t.ApprovalStatus == ApprovalStatus.Pending),
            EndUserSessionCount = sessionList.Count(s => relevantIds.Contains(s.UserId)),
            KnowledgeBaseCount = knowledgeBaseCount
        };
    }
}
