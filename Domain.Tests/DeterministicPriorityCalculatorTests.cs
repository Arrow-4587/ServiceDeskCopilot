using NUnit.Framework;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Services;

namespace Domain.Tests;

[TestFixture]
public class DeterministicPriorityCalculatorTests
{
    [TestCase(IncidentImpact.High, IncidentUrgency.High, IncidentPriority.Critical)]
    [TestCase(IncidentImpact.High, IncidentUrgency.Medium, IncidentPriority.High)]
    [TestCase(IncidentImpact.Medium, IncidentUrgency.High, IncidentPriority.High)]
    [TestCase(IncidentImpact.High, IncidentUrgency.Low, IncidentPriority.Medium)]
    [TestCase(IncidentImpact.Medium, IncidentUrgency.Medium, IncidentPriority.Medium)]
    [TestCase(IncidentImpact.Low, IncidentUrgency.High, IncidentPriority.Medium)]
    [TestCase(IncidentImpact.Medium, IncidentUrgency.Low, IncidentPriority.Low)]
    [TestCase(IncidentImpact.Low, IncidentUrgency.Medium, IncidentPriority.Low)]
    [TestCase(IncidentImpact.Low, IncidentUrgency.Low, IncidentPriority.Low)]
    public void Calculate_ShouldReturnExpectedPriority_ForGivenImpactAndUrgency(
        IncidentImpact impact,
        IncidentUrgency urgency,
        IncidentPriority expectedPriority)
    {
        var result = DeterministicPriorityCalculator.Calculate(impact, urgency);

        Assert.That(result, Is.EqualTo(expectedPriority));
    }
}
