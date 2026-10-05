using LeaveMate.Controllers;
using LeaveMate.Data;
using LeaveMate.DTOs;
using LeaveMate.Enums;
using LeaveMate.Models;
using LeaveMate.Services;
using LeaveMate.Services.Email;
using LeaveMate.Services.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LeaveMate.Tests
{
    public class LeaveRequestEmailNotificationTests
    {
        private static ApplicationDbContext NewDb(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(name)
                .Options;
            return new ApplicationDbContext(options);
        }

        private static (Employee supervisor, Employee employee, Employee hr, Employee hrBackup) SeedOrg(
            ApplicationDbContext db)
        {
            var hr = new Employee { FullName = "HR Admin", Email = "hr@leavemate.test", IsHrAdministrator = true };
            var hrBackup = new Employee { FullName = "HR Backup", Email = "hr-backup@leavemate.test", IsHrAdministrator = true };
            var supervisor = new Employee { FullName = "Supervisor", Email = "supervisor@leavemate.test" };
            db.Employees.AddRange(hr, hrBackup, supervisor);
            db.SaveChanges();

            var employee = new Employee
            {
                FullName = "Requester",
                Email = "requester@leavemate.test",
                SupervisorId = supervisor.Id
            };
            db.Employees.Add(employee);
            db.SaveChanges();

            return (supervisor, employee, hr, hrBackup);
        }

        private static LeaveRequestsController CreateController(
            ApplicationDbContext db,
            IEmailNotificationService emailService) =>
            new(
                db,
                new LeaveValidationService(db),
                new LeaveWorkflowService(db),
                new AuditLogService(db),
                emailService,
                NullLogger<LeaveRequestsController>.Instance);

        private static DateTime NextMonday(DateTime date)
        {
            while (date.DayOfWeek != DayOfWeek.Monday)
            {
                date = date.AddDays(1);
            }

            return date;
        }

        private static LeaveRequest AddRequest(
            ApplicationDbContext db,
            Employee employee,
            LeaveStatus status)
        {
            var startDate = NextMonday(DateTime.UtcNow.Date.AddDays(7));
            var request = new LeaveRequest
            {
                EmployeeId = employee.Id,
                Type = LeaveType.Maternity,
                StartDate = startDate,
                EndDate = startDate.AddDays(2),
                Reason = "Sensitive reason should not be emailed",
                Status = status
            };
            db.LeaveRequests.Add(request);
            db.SaveChanges();
            return request;
        }

        [Fact]
        public async Task Submission_NotifiesDirectSupervisorWithRequestDetails()
        {
            using var db = NewDb(nameof(Submission_NotifiesDirectSupervisorWithRequestDetails));
            var (supervisor, employee, _, _) = SeedOrg(db);
            var email = new RecordingEmailService();
            var startDate = NextMonday(DateTime.UtcNow.Date.AddDays(7));

            var result = await CreateController(db, email).Create(new CreateLeaveRequestDto
            {
                EmployeeId = employee.Id,
                Type = LeaveType.Maternity,
                StartDate = startDate,
                EndDate = startDate.AddDays(2),
                Reason = "Do not include this"
            });

            Assert.IsType<CreatedAtActionResult>(result.Result);
            var notification = Assert.Single(email.Notifications);
            Assert.Equal(supervisor.Email, notification.RecipientEmail);
            Assert.Equal(employee.FullName, notification.RequesterName);
            Assert.Equal(LeaveType.Maternity, notification.LeaveType);
            Assert.Equal(startDate, notification.StartDate);
            Assert.Equal(startDate.AddDays(2), notification.EndDate);
            Assert.Equal(3, notification.DurationInDays);
            Assert.Equal("submitted", notification.Action);
            Assert.Equal(LeaveStatus.PendingSupervisorApproval, notification.CurrentStatus);
            Assert.NotEqual(default, notification.ActionTimeUtc);
        }

        [Theory]
        [InlineData(true, "approved by the supervisor and forwarded to HR", LeaveStatus.PendingHrApproval)]
        [InlineData(false, "rejected by the supervisor", LeaveStatus.Rejected)]
        public async Task SupervisorDecision_NotifiesExpectedRecipients(
            bool approve,
            string expectedAction,
            LeaveStatus expectedStatus)
        {
            using var db = NewDb($"{nameof(SupervisorDecision_NotifiesExpectedRecipients)}_{approve}");
            var (supervisor, employee, hr, hrBackup) = SeedOrg(db);
            var request = AddRequest(db, employee, LeaveStatus.PendingSupervisorApproval);
            var email = new RecordingEmailService();

            var result = await CreateController(db, email).SupervisorDecision(
                request.Id,
                new LeaveDecisionDto { DecidedByEmployeeId = supervisor.Id, Approve = approve });

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(approve ? 2 : 1, email.Notifications.Count);
            Assert.All(email.Notifications, notification =>
            {
                Assert.Equal(employee.FullName, notification.RequesterName);
                Assert.Equal(expectedAction, notification.Action);
                Assert.Equal(expectedStatus, notification.CurrentStatus);
                Assert.Equal(LeaveType.Maternity, notification.LeaveType);
                Assert.Equal(3, notification.DurationInDays);
            });
            IEnumerable<string> expectedRecipients = approve
                ? new[] { hr.Email, hrBackup.Email }
                : new[] { employee.Email };
            Assert.Equal(
                expectedRecipients.OrderBy(address => address),
                email.Notifications.Select(notification => notification.RecipientEmail).OrderBy(address => address));
        }

        [Theory]
        [InlineData(true, "approved by HR", LeaveStatus.Approved)]
        [InlineData(false, "rejected by HR", LeaveStatus.Rejected)]
        public async Task HrDecision_NotifiesRequester(
            bool approve,
            string expectedAction,
            LeaveStatus expectedStatus)
        {
            using var db = NewDb($"{nameof(HrDecision_NotifiesRequester)}_{approve}");
            var (_, employee, hr, _) = SeedOrg(db);
            var request = AddRequest(db, employee, LeaveStatus.PendingHrApproval);
            var email = new RecordingEmailService();

            var result = await CreateController(db, email).HrDecision(
                request.Id,
                new LeaveDecisionDto { DecidedByEmployeeId = hr.Id, Approve = approve });

            Assert.IsType<OkObjectResult>(result);
            var notification = Assert.Single(email.Notifications);
            Assert.Equal(employee.Email, notification.RecipientEmail);
            Assert.Equal(expectedAction, notification.Action);
            Assert.Equal(expectedStatus, notification.CurrentStatus);
            Assert.Equal(hr.FullName, notification.ActorName);
        }

        [Theory]
        [InlineData(LeaveStatus.PendingSupervisorApproval)]
        [InlineData(LeaveStatus.PendingHrApproval)]
        public async Task Recall_NotifiesCurrentApprover(LeaveStatus currentStatus)
        {
            using var db = NewDb($"{nameof(Recall_NotifiesCurrentApprover)}_{currentStatus}");
            var (supervisor, employee, hr, hrBackup) = SeedOrg(db);
            var request = AddRequest(db, employee, currentStatus);
            var email = new RecordingEmailService();

            var result = await CreateController(db, email).Recall(request.Id, employee.Id);

            Assert.IsType<OkObjectResult>(result);
            Assert.All(email.Notifications, notification =>
            {
                Assert.Equal("recalled", notification.Action);
                Assert.Equal(LeaveStatus.Recalled, notification.CurrentStatus);
                Assert.Equal(employee.FullName, notification.ActorName);
            });
            IEnumerable<string> expectedRecipients = currentStatus == LeaveStatus.PendingSupervisorApproval
                ? new[] { supervisor.Email }
                : new[] { hr.Email, hrBackup.Email };
            Assert.Equal(
                expectedRecipients.OrderBy(address => address),
                email.Notifications.Select(notification => notification.RecipientEmail).OrderBy(address => address));
        }

        [Fact]
        public async Task EmailFailure_DoesNotUndoSavedRequestOrAuditRecord()
        {
            using var db = NewDb(nameof(EmailFailure_DoesNotUndoSavedRequestOrAuditRecord));
            var (_, employee, _, _) = SeedOrg(db);
            var email = new RecordingEmailService
            {
                Result = EmailDeliveryResult.Failure("SMTP server unavailable.")
            };
            var startDate = NextMonday(DateTime.UtcNow.Date.AddDays(7));

            var result = await CreateController(db, email).Create(new CreateLeaveRequestDto
            {
                EmployeeId = employee.Id,
                Type = LeaveType.Annual,
                StartDate = startDate,
                EndDate = startDate.AddDays(2)
            });

            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Single(await db.LeaveRequests.ToListAsync());
            Assert.Single(await db.AuditLogs.ToListAsync());
            Assert.Single(email.Notifications);
        }

        private sealed class RecordingEmailService : IEmailNotificationService
        {
            public List<LeaveActionNotification> Notifications { get; } = new();
            public EmailDeliveryResult Result { get; set; } = EmailDeliveryResult.Success();

            public Task<EmailDeliveryResult> SendAsync(
                LeaveActionNotification notification,
                CancellationToken cancellationToken = default)
            {
                Notifications.Add(notification);
                return Task.FromResult(Result);
            }
        }
    }
}
