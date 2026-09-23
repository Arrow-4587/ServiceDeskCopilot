using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Dashboard;
using ServiceDesk.Infrastructure.Persistence;
using ServiceDesk.Web.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace ServiceDesk.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConversationRepository _conversationRepository;
        private readonly IKnowledgeBaseService _knowledgeBaseService;
        private readonly ISystemStatusReader _statusReader;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext dbContext,
            IConversationRepository conversationRepository,
            IKnowledgeBaseService knowledgeBaseService,
            ISystemStatusReader statusReader,
            IWebHostEnvironment env,
            ILogger<HomeController> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
            _knowledgeBaseService = knowledgeBaseService ?? throw new ArgumentNullException(nameof(knowledgeBaseService));
            _statusReader = statusReader ?? throw new ArgumentNullException(nameof(statusReader));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> Index()
        {
            var sw = Stopwatch.StartNew();
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Employee";
            var username = User.Identity?.Name ?? "Employee";
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";

            var model = new HomeViewModel
            {
                UserRole = userRole,
                Username = username,
                UserEmail = userEmail
            };

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _ = Guid.TryParse(userIdString, out Guid userId);

            // Fetch data depending on user role using AsNoTracking for read-only query performance
            if (User.IsInRole("Administrator") || userRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
            {
                model.RegisteredUsers = await _dbContext.Users
                    .AsNoTracking()
                    .OrderBy(u => u.Role)
                    .ThenBy(u => u.Username)
                    .ToListAsync();

                var allConversations = await _conversationRepository.GetAllAsync();
                model.AllChatHistory = allConversations.OrderByDescending(c => c.StartedAt).ToList();
                model.UserChatHistory = (await _conversationRepository.GetByUserIdAsync(userId)).OrderByDescending(c => c.StartedAt).ToList();

                var allTickets = await _dbContext.IncidentDrafts.AsNoTracking().ToListAsync();
                var userDict = model.RegisteredUsers.ToDictionary(u => u.Id, u => u.Username);
                
                model.EmployeeTicketStats = allTickets
                    .GroupBy(t => t.UserId)
                    .ToDictionary(
                        g => userDict.ContainsKey(g.Key) ? userDict[g.Key] : "Unknown", 
                        g => g.Count()
                    );

                model.AuditLogs = await _dbContext.AuditLogs
                    .AsNoTracking()
                    .OrderByDescending(a => a.Timestamp)
                    .Take(250)
                    .ToListAsync();
            }
            else if (userRole.Equals("Analyst", StringComparison.OrdinalIgnoreCase))
            {
                model.RegisteredUsers = await _dbContext.Users.AsNoTracking().ToListAsync();
                var endUserIds = model.RegisteredUsers
                    .Where(u => u.Role == ServiceDesk.Domain.Enums.UserRole.Employee || u.Role == ServiceDesk.Domain.Enums.UserRole.Manager)
                    .Select(u => u.Id)
                    .ToList();

                model.AllTickets = await _dbContext.IncidentDrafts
                    .AsNoTracking()
                    .Where(t => endUserIds.Contains(t.UserId))
                    .ToListAsync();

                var allConversations = await _conversationRepository.GetAllAsync();
                model.AllChatHistory = allConversations.Where(c => endUserIds.Contains(c.UserId)).OrderByDescending(c => c.StartedAt).ToList();
                model.UserChatHistory = (await _conversationRepository.GetByUserIdAsync(userId)).OrderByDescending(c => c.StartedAt).ToList();
            }
            else // Employee or Manager
            {
                model.UserChatHistory = (await _conversationRepository.GetByUserIdAsync(userId)).OrderByDescending(c => c.StartedAt).ToList();
                model.UserTickets = await _dbContext.IncidentDrafts.AsNoTracking().Where(i => i.UserId == userId).ToListAsync();
            }

            // Fetch approved knowledge documents (cached via KnowledgeBaseService)
            try
            {
                model.KnowledgeDocuments = await _knowledgeBaseService.GetApprovedDocumentsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HomeController] Error fetching approved knowledge documents from Azure Blob Storage.");
                model.KnowledgeDocuments = Array.Empty<ServiceDesk.Application.DTOs.Knowledge.KnowledgeDocumentDto>();
            }

            // Fetch service status health for dashboard cards
            try
            {
                model.ServiceStatuses = await _statusReader.GetAllStatusesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HomeController] Error fetching service health statuses.");
                model.ServiceStatuses = Array.Empty<ServiceDesk.Application.DTOs.Status.ServiceStatusDto>();
            }

            if (userRole.Equals("Analyst", StringComparison.OrdinalIgnoreCase))
            {
                model.AnalystMetrics = AnalystDashboardMetrics.From(
                    model.AllTickets,
                    model.AllChatHistory,
                    model.RegisteredUsers
                        .Where(u => u.Role == ServiceDesk.Domain.Enums.UserRole.Employee || u.Role == ServiceDesk.Domain.Enums.UserRole.Manager)
                        .Select(u => u.Id)
                        .ToList(),
                    model.KnowledgeDocuments.Count);
            }

            sw.Stop();
            _logger.LogInformation("[HomeController] Index rendered for role '{Role}' in {ElapsedMs}ms.", userRole, sw.ElapsedMilliseconds);

            return View(model);
        }

        private static string FormatTitleFromFileName(string fileName)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            return nameWithoutExt switch
            {
                "email_outlook_setup" => "Outlook & Office 365 Setup Guide",
                "hardware_request_policy" => "Hardware Request & Provisioning Policy",
                "password_reset_policy" => "Self-Service Password Reset Policy",
                "printer_troubleshooting" => "Printer Spooler & Badge Printing Guide",
                "remote_work_security" => "Remote Work Security Guidelines",
                "security_incident_reporting" => "Security Incident Reporting Protocol",
                "software_install_policy" => "Software Installation & Licensing Policy",
                "teams_mfa_setup" => "Microsoft Teams & MFA Setup Guide",
                "vpn_policy" => "Cisco AnyConnect Corporate VPN Policy",
                "wifi_access_guide" => "Corporate Wi-Fi Access & Guest Setup",
                _ => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(nameWithoutExt.Replace("_", " "))
            };
        }

        private static string GetCategoryFromFileName(string fileName)
        {
            var name = fileName.ToLower();
            if (name.Contains("security") || name.Contains("mfa") || name.Contains("password")) return "Security & Access";
            if (name.Contains("vpn") || name.Contains("wifi") || name.Contains("remote")) return "Network & Connectivity";
            if (name.Contains("hardware") || name.Contains("printer")) return "Devices & Hardware";
            return "Software & Apps";
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
