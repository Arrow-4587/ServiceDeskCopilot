using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceDesk.Application.Services.Agent;

namespace Application.Tests.Agent;

[TestFixture]
public class PlannerServiceTests
{
    private PlannerService _planner = null!;

    [SetUp]
    public void SetUp()
    {
        _planner = new PlannerService(NullLogger<PlannerService>.Instance);
    }

    [Test]
    public async Task PlanAsync_WithVpnQuery_FormulatesVpnTroubleshootingPlan()
    {
        var plan = await _planner.PlanAsync("My VPN is failing to connect with error 403.");

        Assert.That(plan.UserIntent, Is.EqualTo("VpnTroubleshooting"));
        Assert.That(plan.RequiresKnowledgeSearch, Is.True);
        Assert.That(plan.RequiresStatusCheck, Is.True);
        Assert.That(plan.ServiceName, Is.EqualTo("GlobalProtect VPN"));
        Assert.That(plan.RequiresIncidentDraft, Is.False);
    }

    [Test]
    public async Task PlanAsync_WithPasswordQuery_FormulatesIdentityPlan()
    {
        var plan = await _planner.PlanAsync("How do I reset my domain password?");

        Assert.That(plan.UserIntent, Is.EqualTo("PasswordManagement"));
        Assert.That(plan.RequiresKnowledgeSearch, Is.True);
        Assert.That(plan.DraftCategory, Is.EqualTo("Identity & Access"));
    }

    [Test]
    public async Task PlanAsync_WithExplicitTicketRequest_MarksDraftRequired()
    {
        var plan = await _planner.PlanAsync("Please raise ticket for broken laptop screen");

        Assert.That(plan.RequiresIncidentDraft, Is.True);
        Assert.That(plan.RequiresKnowledgeSearch, Is.False);
    }

    [Test]
    public async Task PlanAsync_WithEmptyQuery_ReturnsClarificationPlan()
    {
        var plan = await _planner.PlanAsync("");

        Assert.That(plan.UserIntent, Is.EqualTo("EmptyQuery"));
        Assert.That(plan.RequiresKnowledgeSearch, Is.False);
    }
}
