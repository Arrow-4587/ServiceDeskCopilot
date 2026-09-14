using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Services;

/// <summary>
/// Domain service responsible for computing ticket priority deterministically based on Impact and Urgency matrix.
/// Priority rules strictly belong to Domain and cannot be overridden by AI agents.
/// </summary>
public static class DeterministicPriorityCalculator
{
    public static IncidentPriority Calculate(IncidentImpact impact, IncidentUrgency urgency)
    {
        return (impact, urgency) switch
        {
            (IncidentImpact.High, IncidentUrgency.High) => IncidentPriority.Critical,
            
            (IncidentImpact.High, IncidentUrgency.Medium) => IncidentPriority.High,
            (IncidentImpact.Medium, IncidentUrgency.High) => IncidentPriority.High,
            
            (IncidentImpact.High, IncidentUrgency.Low) => IncidentPriority.Medium,
            (IncidentImpact.Medium, IncidentUrgency.Medium) => IncidentPriority.Medium,
            (IncidentImpact.Low, IncidentUrgency.High) => IncidentPriority.Medium,
            
            (IncidentImpact.Medium, IncidentUrgency.Low) => IncidentPriority.Low,
            (IncidentImpact.Low, IncidentUrgency.Medium) => IncidentPriority.Low,
            (IncidentImpact.Low, IncidentUrgency.Low) => IncidentPriority.Low,
            
            _ => IncidentPriority.Low
        };
    }
}
