using Microsoft.EntityFrameworkCore;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext dbContext)
    {
        if (await dbContext.Users.AnyAsync())
            return; // Data already seeded

        var users = new List<User>
        {
            new User("employee1", "employee@company.com", UserRole.Employee),
            new User("analyst1", "analyst@company.com", UserRole.Analyst),
            new User("manager1", "manager@company.com", UserRole.Manager),
            new User("admin1", "admin@company.com", UserRole.Administrator)
        };

        await dbContext.Users.AddRangeAsync(users);
        await dbContext.SaveChangesAsync();
    }
}
