using LeaveMate.Data;
using LeaveMate.Services;
using LeaveMate.Services.Validation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Data Access Engineer track owns the real connection string / migrations;
// this wires the backend services to whatever SQL Server instance is configured.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Backend track services: validation engine + workflow state machine.
builder.Services.AddScoped<ILeaveValidationService, LeaveValidationService>();
builder.Services.AddScoped<LeaveWorkflowService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
