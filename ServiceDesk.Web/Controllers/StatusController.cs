using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Common.Interfaces;

namespace ServiceDesk.Web.Controllers;

[Authorize]
public class StatusController : Controller
{
    private readonly ISystemStatusReader _statusReader;
    private readonly ILogger<StatusController> _logger;

    public StatusController(
        ISystemStatusReader statusReader,
        ILogger<StatusController> logger)
    {
        _statusReader = statusReader ?? throw new ArgumentNullException(nameof(statusReader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var statuses = await _statusReader.GetAllStatusesAsync(cancellationToken);
            return View(statuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching service status dashboard items.");
            return View(Array.Empty<ServiceDesk.Application.DTOs.Status.ServiceStatusDto>());
        }
    }
}
