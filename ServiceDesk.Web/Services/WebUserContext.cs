using System.Security.Claims;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Web.Services;

public class WebUserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WebUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public Guid UserId
    {
        get
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }
    }

    public string Username => User?.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

    public string Email => User?.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

    public UserRole Role
    {
        get
        {
            var roleClaim = User?.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var role) ? role : UserRole.Employee;
        }
    }
}
