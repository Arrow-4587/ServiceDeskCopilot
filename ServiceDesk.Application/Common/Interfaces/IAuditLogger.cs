using System.Threading;
using System.Threading.Tasks;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IAuditLogger
{
    Task LogActionAsync(string action, string userId, string userRole, string details, string ipAddress = "", CancellationToken cancellationToken = default);
    Task LogActionAsync(string action, string userId, string details, string ipAddress = "", CancellationToken cancellationToken = default);
}
