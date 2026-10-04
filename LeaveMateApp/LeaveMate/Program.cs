using LeaveMate.Data;
using LeaveMate.Middleware;
using LeaveMate.Services;
using LeaveMate.Services.Integration;
using LeaveMate.Services.Validation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// --- Data Access (Data Access Engineer / DBA tracks) -----------------------
// SQLite is used by default so the app runs with zero external setup;
// flip "UseSqlite": false in appsettings.json to target the SQL Server
// connection string instead, per the proposal's production stack.
var useSqlite = builder.Configuration.GetValue<bool>("UseSqlite");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useSqlite)
        options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite"));
    else
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// --- Backend track: validation engine + workflow state machine -------------
builder.Services.AddScoped<ILeaveValidationService, LeaveValidationService>();
builder.Services.AddScoped<LeaveWorkflowService>();

// --- Backend track: background coverage matrix refresh (async handlers) ---
builder.Services.AddSingleton<CoverageCache>();
builder.Services.AddHostedService<CoverageRefreshService>();

// --- Systems Integrator track: typed client wiring UI to controllers ------
builder.Services.AddHttpClient<LeaveMateApiClient>(client =>
{
    // Self-referencing base address: Razor Pages call this app's own API.
    // Determine the listening port for local loopback (127.0.0.1).
    // NEVER use '+' or '*' or '0.0.0.0' as the client host name because HttpClient cannot resolve them.
    var appBaseUrl = builder.Configuration["AppBaseUrl"];
    if (string.IsNullOrWhiteSpace(appBaseUrl))
    {
        var effectivePort = "5000";
        if (!string.IsNullOrEmpty(port))
        {
            effectivePort = port;
        }
        else
        {
            var rawUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
                ?? builder.Configuration["urls"];
            if (!string.IsNullOrEmpty(rawUrls))
            {
                var match = System.Text.RegularExpressions.Regex.Match(rawUrls, @":(\d+)");
                if (match.Success)
                {
                    effectivePort = match.Groups[1].Value;
                }
            }
        }
        appBaseUrl = $"http://127.0.0.1:{effectivePort}";
    }

    var cleanUrl = appBaseUrl.Split(';', StringSplitOptions.RemoveEmptyEntries)[0]
        .Replace("://+:", "://127.0.0.1:")
        .Replace("://*:", "://127.0.0.1:")
        .Replace("://0.0.0.0:", "://127.0.0.1:")
        .TrimEnd('/') + "/";

    client.BaseAddress = new Uri(cleanUrl);
});

var app = builder.Build();

// --- Seed on startup for a working demo out of the box ---------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    DbSeeder.Seed(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapPost("/Account/SignOut", (HttpContext context) =>
{
    context.Session.Clear();
    return Results.Redirect("/Account/Login");
});

app.MapControllers();
app.MapRazorPages();
app.MapControllerRoute(
    name: "portal",
    pattern: "Portal/{controller=Home}/{action=Index}/{id?}");

app.Run();
