using Microsoft.EntityFrameworkCore;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext dbContext)
    {
        await dbContext.Database.MigrateAsync();

        if (!await dbContext.Users.AnyAsync())
        {
            var users = new List<User>
            {
                new User("employee1", "employee@company.com", UserRole.Employee, "Employee123!"),
                new User("analyst1", "analyst@company.com", UserRole.Analyst, "Analyst123!"),
                new User("manager1", "manager@company.com", UserRole.Manager, "Manager123!"),
                new User("admin1", "admin@company.com", UserRole.Administrator, "Admin123!")
            };

            await dbContext.Users.AddRangeAsync(users);
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.AuditLogs.AnyAsync())
        {
            var sampleLogs = new List<AuditLog>
            {
                new AuditLog("UserLogin", "admin1", "Administrator", "Successful login via Enterprise Web Portal", "192.168.1.10"),
                new AuditLog("AddSystemPrompt", "admin1", "Administrator", "Added prompt override for Analyst role", "192.168.1.10"),
                new AuditLog("ToggleTool", "admin1", "Administrator", "Enabled Tool: JiraTicketSync for Manager role", "192.168.1.10"),
                new AuditLog("UserLogin", "manager1", "Manager", "Successful login via Enterprise Web Portal", "192.168.1.45"),
                new AuditLog("ApproveIncident", "manager1", "Manager", "Approved incident draft INC-2026-0842 (Exchange Online Latency)", "192.168.1.45"),
                new AuditLog("RejectIncident", "manager1", "Manager", "Rejected duplicate incident draft INC-2026-0839", "192.168.1.45"),
                new AuditLog("UserLogin", "analyst1", "Analyst", "Successful login via Enterprise Web Portal", "192.168.1.72"),
                new AuditLog("InspectIncident", "analyst1", "Analyst", "Inspected triage diagnostic logs for VPN gateway node 04", "192.168.1.72"),
                new AuditLog("ChatQuery", "analyst1", "Analyst", "Queried grounded knowledge for GlobalProtect gateway failover policy", "192.168.1.72"),
                new AuditLog("UserLogin", "employee1", "Employee", "Successful login via SSO", "10.0.4.18"),
                new AuditLog("ChatQuery", "employee1", "Employee", "Asked Copilot: 'VPN connection keeps dropping during video calls'", "10.0.4.18"),
                new AuditLog("IncidentCreated", "employee1", "Employee", "Created draft ticket: VPN Disconnection & SSL Certificate Timeout", "10.0.4.18"),
                new AuditLog("ChatQuery", "employee1", "Employee", "Requested SSPR self-service password unlock link", "10.0.4.18"),
                new AuditLog("UserLogout", "employee1", "Employee", "User logged out successfully", "10.0.4.18")
            };

            await dbContext.AuditLogs.AddRangeAsync(sampleLogs);
            await dbContext.SaveChangesAsync();
        }
    }
}
