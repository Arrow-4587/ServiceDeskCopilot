using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Infrastructure;
using ServiceDesk.Infrastructure.Persistence;
using ServiceDesk.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Support direct DLL launches from outside the web project directory.
// Environment variables remain the highest-priority override.
if (string.IsNullOrWhiteSpace(builder.Configuration["AzureOpenAI:Endpoint"]) ||
    string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
{
    var deployedSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    if (File.Exists(deployedSettingsPath))
    {
        builder.Configuration.AddJsonFile(deployedSettingsPath, optional: false, reloadOnChange: false);

        if (builder.Environment.IsDevelopment())
        {
            var deployedDevelopmentSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.Development.json");
            if (File.Exists(deployedDevelopmentSettingsPath))
            {
                builder.Configuration.AddJsonFile(deployedDevelopmentSettingsPath, optional: false, reloadOnChange: false);
            }
        }
    }
}

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Register Clean Architecture Layer Services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// User Context Adapter
builder.Services.AddScoped<IUserContext, WebUserContext>();

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "ServiceDeskCopilot.Auth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Role-Based Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireEmployee", policy =>
        policy.RequireRole(UserRole.Employee.ToString(), UserRole.Analyst.ToString(), UserRole.Manager.ToString(), UserRole.Administrator.ToString()));

    options.AddPolicy("RequireAnalyst", policy =>
        policy.RequireRole(UserRole.Analyst.ToString(), UserRole.Manager.ToString(), UserRole.Administrator.ToString()));

    options.AddPolicy("RequireManager", policy =>
        policy.RequireRole(UserRole.Manager.ToString(), UserRole.Administrator.ToString()));

    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole(UserRole.Administrator.ToString()));
});

// Add Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Apply schema migrations and ingest approved documents from Azure Blob Storage into Azure AI Search.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();

    var ingestionService = scope.ServiceProvider.GetRequiredService<IKnowledgeIngestionService>();
    await ingestionService.IngestApprovedKnowledgeDocumentsAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
