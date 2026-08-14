using LeaveMate.Data;
using LeaveMate.Services;
using LeaveMate.Services.Notifications;
using LeaveMate.Services.Validation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// Database
// --------------------------------------------------

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// --------------------------------------------------
// Controllers
// --------------------------------------------------

builder.Services.AddControllers();

// --------------------------------------------------
// Swagger
// --------------------------------------------------

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --------------------------------------------------
// Application Services
// --------------------------------------------------

builder.Services.AddScoped<LeaveWorkflowService>();

builder.Services.AddScoped<ILeaveValidationService, LeaveValidationService>();

builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Notification Services
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<LeaveNotificationHandler>();

// --------------------------------------------------
// CORS
// --------------------------------------------------

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// --------------------------------------------------
// Build application
// --------------------------------------------------

var app = builder.Build();

// --------------------------------------------------
// Middleware
// --------------------------------------------------

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.MapControllers();

// --------------------------------------------------
// Run
// --------------------------------------------------

app.Run();