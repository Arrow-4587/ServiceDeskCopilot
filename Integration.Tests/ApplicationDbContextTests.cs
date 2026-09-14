using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.ValueObjects;
using ServiceDesk.Infrastructure.Persistence;

namespace Integration.Tests;

[TestFixture]
public class ApplicationDbContextTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Test]
    public async Task CanAddAndRetrieveUser()
    {
        using var context = CreateInMemoryDbContext();
        var user = new User("johndoe", "johndoe@company.com", UserRole.Employee);

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var retrieved = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Username, Is.EqualTo("johndoe"));
        Assert.That(retrieved.Role, Is.EqualTo(UserRole.Employee));
    }

    [Test]
    public async Task CanAddAndRetrieveConversationWithCitations()
    {
        using var context = CreateInMemoryDbContext();
        var userId = Guid.NewGuid();
        var session = new ConversationSession(userId);

        var citations = new List<Citation>
        {
            new Citation("VPN Guide", "Sec 1", 2, "Reboot router", "data/knowledge/vpn.md")
        };

        session.AddMessage("User", "How do I fix VPN?", null);
        session.AddMessage("Assistant", "Try rebooting your router.", citations);

        await context.ConversationSessions.AddAsync(session);
        await context.SaveChangesAsync();

        var retrieved = await context.ConversationSessions
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == session.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Messages.Count, Is.EqualTo(2));

        var assistantMsg = retrieved.Messages.Last();
        Assert.That(assistantMsg.Citations.Count, Is.EqualTo(1));
        Assert.That(assistantMsg.Citations[0].DocumentName, Is.EqualTo("VPN Guide"));
    }
}
