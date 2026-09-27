using LeaveMate.Data;
using LeaveMate.Middleware;
using LeaveMate.Services;
using LeaveMate.Services.Integration;
using LeaveMate.Services.Validation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
    client.BaseAddress = new Uri(builder.Configuration["AppBaseUrl"] ?? "http://localhost:5000");
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
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();
