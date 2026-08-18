using System;
using System.Threading.Tasks;
using LeaveMate.Data;
using LeaveMate.DTOs;
using LeaveMate.Enums;
using LeaveMate.Models;
using LeaveMate.Services;
using LeaveMate.Services.Notifications;
using LeaveMate.Services.Validation;
using LeaveMate.Controllers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace LEAVEMATE_BACKEND.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task CreateLeaveRequest_SavesRequestAuditAndSendsNotification()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var employee = new Employee
        {
            Id = 1,
            FullName = "John Doe",
            Email = "john@leavemate.com",
            AnnualLeaveBalanceDays = 21
        };

        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        var fakeNotificationService = new FakeNotificationService();

        var notificationHandler =
            new LeaveNotificationHandler(fakeNotificationService);

        var validationService =
            new LeaveValidationService(db);

        var workflowService =
            new LeaveWorkflowService(db);

        var auditLogService =
            new AuditLogService(db);

        var controller = new LeaveRequestsController(
            db,
            validationService,
            workflowService,
            notificationHandler,
            auditLogService);

        var startDate = GetNextWeekday();
        var endDate = startDate.AddDays(1);

        var dto = new CreateLeaveRequestDto
        {
            EmployeeId = employee.Id,
            Type = LeaveType.Annual,
            StartDate = startDate,
            EndDate = endDate,
            Reason = "Family event"
        };

        var result = await controller.Create(dto);

        var createdResult =
            Assert.IsType<CreatedAtActionResult>(result.Result);

        Assert.NotNull(createdResult.Value);

        var savedRequest =
            await db.LeaveRequests
                .FirstOrDefaultAsync();

        Assert.NotNull(savedRequest);

        Assert.Equal(
            LeaveStatus.PendingSupervisorApproval,
            savedRequest!.Status);

        var auditLog =
            await db.AuditLogs
                .FirstOrDefaultAsync();

        Assert.NotNull(auditLog);

        Assert.Equal(
            savedRequest.Id,
            auditLog!.LeaveRequestId);

        Assert.Equal(
            employee.Id,
            auditLog.PerformedByEmployeeId);

        Assert.Equal(
            "Leave Request Submitted",
            auditLog.Action);

        Assert.Equal(
            "john@leavemate.com",
            fakeNotificationService.Recipient);

        Assert.Equal(
            "Leave Request Submitted",
            fakeNotificationService.Subject);

        Assert.Contains(
            $"Leave request #{savedRequest.Id}",
            fakeNotificationService.Message);
    }

    [Fact]
    public async Task SupervisorDecision_ApprovesRequest_CreatesAuditAndSendsNotification()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var supervisor = new Employee
        {
            Id = 1,
            FullName = "Jane Supervisor",
            Email = "supervisor@leavemate.com",
            AnnualLeaveBalanceDays = 21
        };

        var employee = new Employee
        {
            Id = 2,
            FullName = "John Doe",
            Email = "john@leavemate.com",
            SupervisorId = supervisor.Id,
            AnnualLeaveBalanceDays = 21
        };

        db.Employees.AddRange(
            supervisor,
            employee);

        var startDate = GetNextWeekday();

        var request = new LeaveRequest
        {
            EmployeeId = employee.Id,
            Employee = employee,
            Type = LeaveType.Annual,
            StartDate = startDate,
            EndDate = startDate.AddDays(1),
            Reason = "Family event",
            Status = LeaveStatus.PendingSupervisorApproval
        };

        db.LeaveRequests.Add(request);

        await db.SaveChangesAsync();

        var fakeNotificationService =
            new FakeNotificationService();

        var notificationHandler =
            new LeaveNotificationHandler(
                fakeNotificationService);

        var validationService =
            new LeaveValidationService(db);

        var workflowService =
            new LeaveWorkflowService(db);

        var auditLogService =
            new AuditLogService(db);

        var controller =
            new LeaveRequestsController(
                db,
                validationService,
                workflowService,
                notificationHandler,
                auditLogService);

        var dto = new LeaveDecisionDto
        {
            DecidedByEmployeeId = supervisor.Id,
            Approve = true,
            Comment = "Approved by supervisor"
        };

        var result =
            await controller.SupervisorDecision(
                request.Id,
                dto);

        Assert.IsType<OkObjectResult>(result);

        var savedRequest =
            await db.LeaveRequests
                .FirstAsync(r => r.Id == request.Id);

        Assert.Equal(
            LeaveStatus.PendingHrApproval,
            savedRequest.Status);

        Assert.Equal(
            supervisor.Id,
            savedRequest.LastActionedByEmployeeId);

        Assert.Equal(
            "Approved by supervisor",
            savedRequest.SupervisorComment);

        var auditLog =
            await db.AuditLogs
                .FirstOrDefaultAsync(
                    a => a.LeaveRequestId == request.Id);

        Assert.NotNull(auditLog);

        Assert.Equal(
            supervisor.Id,
            auditLog!.PerformedByEmployeeId);

        Assert.Equal(
            "Supervisor Approved Leave",
            auditLog.Action);

        Assert.Equal(
            "Approved by supervisor",
            auditLog.Details);

        Assert.Equal(
            "john@leavemate.com",
            fakeNotificationService.Recipient);

        Assert.Equal(
            "Leave Request Approved",
            fakeNotificationService.Subject);

        Assert.Contains(
            "Approved",
            fakeNotificationService.Message);
    }

    private static DateTime GetNextWeekday()
    {
        var date = DateTime.UtcNow.Date.AddDays(1);

        while (
            date.DayOfWeek == DayOfWeek.Saturday ||
            date.DayOfWeek == DayOfWeek.Sunday)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private sealed class FakeNotificationService
        : INotificationService
    {
        public bool WasSent { get; private set; }

        public string? Recipient { get; private set; }

        public string? Subject { get; private set; }

        public string? Message { get; private set; }

        public Task SendAsync(
            string recipient,
            string subject,
            string message)
        {
            WasSent = true;
            Recipient = recipient;
            Subject = subject;
            Message = message;

            return Task.CompletedTask;
        }
    }
}