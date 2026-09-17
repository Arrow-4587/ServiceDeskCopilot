using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Infrastructure.Persistence;
using ServiceDesk.Infrastructure.Persistence.Repositories;

namespace Integration.Tests;

[TestFixture]
public class IncidentDraftRepositoryTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Test]
    public async Task AddAndGetById_ShouldPersistDraftWithComputedPriority()
    {
        using var dbContext = CreateInMemoryDbContext();
        var repository = new IncidentDraftRepository(dbContext);

        var userId = Guid.NewGuid();
        var draft = new IncidentDraft(userId, "Server Down", "Domain Controller 01 unresponsive", "Infrastructure", IncidentImpact.High, IncidentUrgency.High);

        await repository.AddAsync(draft);

        var retrieved = await repository.GetByIdAsync(draft.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Title, Is.EqualTo("Server Down"));
        Assert.That(retrieved.ComputedPriority, Is.EqualTo(IncidentPriority.Critical));
    }

    [Test]
    public async Task UpdateAsync_ShouldPersistApprovalAndSubmittedTicketId()
    {
        using var dbContext = CreateInMemoryDbContext();
        var repository = new IncidentDraftRepository(dbContext);

        var userId = Guid.NewGuid();
        var draft = new IncidentDraft(userId, "Email Down", "Exchange offline", "Software", IncidentImpact.Medium, IncidentUrgency.Medium);
        await repository.AddAsync(draft);

        draft.ApproveByAuthenticatedUser();
        draft.MarkSubmitted("INC-88888");

        await repository.UpdateAsync(draft);

        var retrieved = await repository.GetByIdAsync(draft.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.ApprovalStatus, Is.EqualTo(ApprovalStatus.Approved));
        Assert.That(retrieved.SubmittedIncidentId, Is.EqualTo("INC-88888"));
        Assert.That(retrieved.Status, Is.EqualTo(IncidentStatus.Submitted));
    }
}
