using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Web.Services;

namespace Integration.Tests.Security;

[TestFixture]
public class WebUserContextTests
{
    [Test]
    public void IsAuthenticated_ShouldBeFalse_WhenNoUserInHttpContext()
    {
        var httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var userContext = new WebUserContext(httpContextAccessor);

        Assert.That(userContext.IsAuthenticated, Is.False);
        Assert.That(userContext.UserId, Is.EqualTo(Guid.Empty));
        Assert.That(userContext.Role, Is.EqualTo(UserRole.Employee));
    }

    [Test]
    public void Properties_ShouldReflectClaimsPrincipal_WhenUserIsAuthenticated()
    {
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "analyst_user"),
            new Claim(ClaimTypes.Email, "analyst@company.com"),
            new Claim(ClaimTypes.Role, UserRole.Analyst.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestCookie");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        var userContext = new WebUserContext(httpContextAccessor);

        Assert.That(userContext.IsAuthenticated, Is.True);
        Assert.That(userContext.UserId, Is.EqualTo(userId));
        Assert.That(userContext.Username, Is.EqualTo("analyst_user"));
        Assert.That(userContext.Email, Is.EqualTo("analyst@company.com"));
        Assert.That(userContext.Role, Is.EqualTo(UserRole.Analyst));
    }
}
