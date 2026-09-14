using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using NUnit.Framework;
using ServiceDesk.Domain.Enums;

namespace Integration.Tests.Security;

[TestFixture]
public class CookieAuthenticationTests
{
    [TestCase(UserRole.Employee)]
    [TestCase(UserRole.Analyst)]
    [TestCase(UserRole.Manager)]
    [TestCase(UserRole.Administrator)]
    public void ClaimsIdentity_ShouldContainCorrectRoleClaim_ForEveryUserRole(UserRole role)
    {
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, $"user_{role}"),
            new Claim(ClaimTypes.Email, $"{role.ToString().ToLower()}@company.com"),
            new Claim(ClaimTypes.Role, role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        Assert.That(principal.Identity?.IsAuthenticated, Is.True);
        Assert.That(principal.IsInRole(role.ToString()), Is.True);
        Assert.That(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, Is.EqualTo(userId.ToString()));
    }
}
