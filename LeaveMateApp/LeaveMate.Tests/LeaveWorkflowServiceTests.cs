using LeaveMate.Data;
using LeaveMate.Enums;
using LeaveMate.Exceptions;
using LeaveMate.Models;
using LeaveMate.Services;
using LeaveMate.Services.Email;
using LeaveMate.Services.Validation;
using LeaveMate.Controllers;
using LeaveMate.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LeaveMate.Tests
{
    public class LeaveWorkflowServiceTests
    {
        private static ApplicationDbContext NewInMemoryDb(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(name)
                .Options;
            return new ApplicationDbContext(options);
        }

        private static (Employee supervisor, Employee employee, Employee hr) SeedOrg(ApplicationDbContext db)
        {
            var hr = new Employee { FullName = "HR Admin", Email = "hr@x.com", IsHrAdministrator = true };
            var supervisor = new Employee { FullName = "Supervisor", Email = "sup@x.com" };
            db.Employees.AddRange(hr, supervisor);
            db.SaveChanges();

            var employee = new Employee { FullName = "Employee", Email = "emp@x.com", SupervisorId = supervisor.Id };
            db.Employees.Add(employee);
            db.SaveChanges();

            return (supervisor, employee, hr);
        }

        [Fact]
        public async Task SupervisorApproval_MovesToHrTier()
        {
            using var db = NewInMemoryDb(nameof(SupervisorApproval_MovesToHrTier));
            var (supervisor, employee, _) = SeedOrg(db);
            var workflow = new LeaveWorkflowService(db);

            var request = new LeaveRequest { EmployeeId = employee.Id, Status = LeaveStatus.PendingSupervisorApproval };

            await workflow.ApplySupervisorDecisionAsync(request, supervisor.Id, approve: true, comment: "OK");

            Assert.Equal(LeaveStatus.PendingHrApproval, request.Status);
        }

        [Fact]
        public async Task SupervisorRejection_EndsWorkflow()
        {
            using var db = NewInMemoryDb(nameof(SupervisorRejection_EndsWorkflow));
            var (supervisor, employee, _) = SeedOrg(db);
            var workflow = new LeaveWorkflowService(db);

            var request = new LeaveRequest { EmployeeId = employee.Id, Status = LeaveStatus.PendingSupervisorApproval };

            await workflow.ApplySupervisorDecisionAsync(request, supervisor.Id, approve: false, comment: "No coverage");

            Assert.Equal(LeaveStatus.Rejected, request.Status);
            Assert.NotNull(request.DecidedAtUtc);
        }

        [Fact]
        public async Task NonSupervisor_CannotActionSupervisorTier()
        {
            using var db = NewInMemoryDb(nameof(NonSupervisor_CannotActionSupervisorTier));
            var (_, employee, hr) = SeedOrg(db);
            var workflow = new LeaveWorkflowService(db);

            var request = new LeaveRequest { EmployeeId = employee.Id, Status = LeaveStatus.PendingSupervisorApproval };

            await Assert.ThrowsAsync<WorkflowException>(() =>
                workflow.ApplySupervisorDecisionAsync(request, hr.Id, approve: true, comment: null));
        }

        [Fact]
        public async Task HrApproval_CompletesWorkflow()
        {
            using var db = NewInMemoryDb(nameof(HrApproval_CompletesWorkflow));
            var (_, employee, hr) = SeedOrg(db);
            employee.AnnualLeaveBalanceDays = 12;
            var workflow = new LeaveWorkflowService(db);

            var request = new LeaveRequest
            {
                EmployeeId = employee.Id,
                Type = LeaveType.Annual,
                StartDate = new DateTime(2026, 10, 5),
                EndDate = new DateTime(2026, 10, 7),
                Status = LeaveStatus.PendingHrApproval
            };

            await workflow.ApplyHrDecisionAsync(request, hr.Id, approve: true, comment: "Approved");

            Assert.Equal(LeaveStatus.Approved, request.Status);
            await db.SaveChangesAsync();
            await db.Entry(employee).ReloadAsync();
            Assert.Equal(9, employee.AnnualLeaveBalanceDays);
        }

        [Fact]
        public async Task HrApproval_NonAnnualLeaveDoesNotDeductAnnualBalance()
        {
            using var db = NewInMemoryDb(nameof(HrApproval_NonAnnualLeaveDoesNotDeductAnnualBalance));
            var (_, employee, hr) = SeedOrg(db);
            employee.AnnualLeaveBalanceDays = 12;
            var workflow = new LeaveWorkflowService(db);
            var request = new LeaveRequest
            {
                EmployeeId = employee.Id,
                Type = LeaveType.Sick,
                StartDate = new DateTime(2026, 10, 5),
                EndDate = new DateTime(2026, 10, 7),
                Status = LeaveStatus.PendingHrApproval
            };

            await workflow.ApplyHrDecisionAsync(request, hr.Id, approve: true, comment: "Approved");

            Assert.Equal(LeaveStatus.Approved, request.Status);
            Assert.Equal(12, employee.AnnualLeaveBalanceDays);
        }

        [Theory]
        [InlineData(LeaveType.Sick)]
        [InlineData(LeaveType.Personal)]
        public async Task HrApproval_DeductsMatchingSickOrPersonalBalance(LeaveType type)
        {
            using var db = NewInMemoryDb($"{nameof(HrApproval_DeductsMatchingSickOrPersonalBalance)}_{type}");
            var (_, employee, hr) = SeedOrg(db);
            employee.AnnualLeaveBalanceDays = 12;
            employee.SickLeaveBalanceDays = 10;
            employee.PersonalLeaveBalanceDays = 8;
            var workflow = new LeaveWorkflowService(db);
            var request = new LeaveRequest
            {
                EmployeeId = employee.Id,
                Type = type,
                StartDate = new DateTime(2026, 10, 5),
                EndDate = new DateTime(2026, 10, 7),
                Status = LeaveStatus.PendingHrApproval
            };

            await workflow.ApplyHrDecisionAsync(request, hr.Id, approve: true, comment: "Approved");
            await db.SaveChangesAsync();
            await db.Entry(employee).ReloadAsync();

            Assert.Equal(LeaveStatus.Approved, request.Status);
            Assert.Equal(12, employee.AnnualLeaveBalanceDays);
            Assert.Equal(type == LeaveType.Sick ? 7 : 10, employee.SickLeaveBalanceDays);
            Assert.Equal(type == LeaveType.Personal ? 5 : 8, employee.PersonalLeaveBalanceDays);
        }

        [Fact]
        public async Task HrRejection_DoesNotDeductAnnualLeaveBalance()
        {
            using var db = NewInMemoryDb(nameof(HrRejection_DoesNotDeductAnnualLeaveBalance));
            var (_, employee, hr) = SeedOrg(db);
            employee.AnnualLeaveBalanceDays = 12;
            var workflow = new LeaveWorkflowService(db);
            var request = new LeaveRequest
            {
                EmployeeId = employee.Id,
                Type = LeaveType.Annual,
                StartDate = new DateTime(2026, 10, 5),
                EndDate = new DateTime(2026, 10, 7),
                Status = LeaveStatus.PendingHrApproval
            };

            await workflow.ApplyHrDecisionAsync(request, hr.Id, approve: false, comment: "Rejected");

            Assert.Equal(LeaveStatus.Rejected, request.Status);
            Assert.Equal(12, employee.AnnualLeaveBalanceDays);
        }

        [Fact]
        public async Task HrApproval_WithInsufficientAnnualBalance_IsRejected()
        {
            using var db = NewInMemoryDb(nameof(HrApproval_WithInsufficientAnnualBalance_IsRejected));
            var (_, employee, hr) = SeedOrg(db);
            employee.AnnualLeaveBalanceDays = 2;
            var workflow = new LeaveWorkflowService(db);
            var request = new LeaveRequest
            {
                EmployeeId = employee.Id,
                Type = LeaveType.Annual,
                StartDate = new DateTime(2026, 10, 5),
                EndDate = new DateTime(2026, 10, 7),
                Status = LeaveStatus.PendingHrApproval
            };

            await Assert.ThrowsAsync<WorkflowException>(() =>
                workflow.ApplyHrDecisionAsync(request, hr.Id, approve: true, comment: "Approved"));

            Assert.Equal(LeaveStatus.PendingHrApproval, request.Status);
            Assert.Equal(2, employee.AnnualLeaveBalanceDays);
        }

        [Fact]
        public void Recall_ByOwner_Succeeds()
        {
            using var db = NewInMemoryDb(nameof(Recall_ByOwner_Succeeds));
            var (_, employee, _) = SeedOrg(db);
            var workflow = new LeaveWorkflowService(db);

            var request = new LeaveRequest { EmployeeId = employee.Id, Status = LeaveStatus.PendingSupervisorApproval };

            workflow.Recall(request, employee.Id);

            Assert.Equal(LeaveStatus.Recalled, request.Status);
        }

        [Fact]
        public void Recall_ByNonOwner_Throws()
        {
            using var db = NewInMemoryDb(nameof(Recall_ByNonOwner_Throws));
            var (supervisor, employee, _) = SeedOrg(db);
            var workflow = new LeaveWorkflowService(db);

            var request = new LeaveRequest { EmployeeId = employee.Id, Status = LeaveStatus.PendingSupervisorApproval };

            Assert.Throws<WorkflowException>(() => workflow.Recall(request, supervisor.Id));
        }

        [Fact]
        public void Recall_AfterApproval_Throws()
        {
            using var db = NewInMemoryDb(nameof(Recall_AfterApproval_Throws));
            var (_, employee, _) = SeedOrg(db);
            var workflow = new LeaveWorkflowService(db);

            var request = new LeaveRequest { EmployeeId = employee.Id, Status = LeaveStatus.Approved };

            Assert.Throws<WorkflowException>(() => workflow.Recall(request, employee.Id));
        }
    }

    public class LeaveRequestsControllerAuditTests
        {
            private static ApplicationDbContext NewInMemoryDb(string name)
            {
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(name)
                    .Options;
                return new ApplicationDbContext(options);
            }

            private static (Employee supervisor, Employee employee, Employee hr) SeedOrg(ApplicationDbContext db)
            {
                var hr = new Employee { FullName = "HR Admin", Email = "hr@audit.test", IsHrAdministrator = true };
                var supervisor = new Employee { FullName = "Supervisor", Email = "supervisor@audit.test" };
                db.Employees.AddRange(hr, supervisor);
                db.SaveChanges();

                var employee = new Employee
                {
                    FullName = "Employee",
                    Email = "employee@audit.test",
                    SupervisorId = supervisor.Id
                };
                db.Employees.Add(employee);
                db.SaveChanges();

                return (supervisor, employee, hr);
            }

            private static LeaveRequestsController CreateController(ApplicationDbContext db) =>
                new(
                    db,
                    new LeaveValidationService(db),
                    new LeaveWorkflowService(db),
                    new AuditLogService(db),
                    new DisabledEmailNotificationService(),
                    NullLogger<LeaveRequestsController>.Instance);

            private sealed class DisabledEmailNotificationService : IEmailNotificationService
            {
                public Task<EmailDeliveryResult> SendAsync(
                    LeaveActionNotification notification,
                    CancellationToken cancellationToken = default) =>
                    Task.FromResult(EmailDeliveryResult.Skip());
            }

            private static LeaveRequest AddRequest(
                ApplicationDbContext db,
                Employee employee,
                LeaveStatus status)
            {
                var request = new LeaveRequest
                {
                    EmployeeId = employee.Id,
                    Type = LeaveType.Annual,
                    StartDate = NextMonday(DateTime.UtcNow.Date.AddDays(7)),
                    EndDate = NextMonday(DateTime.UtcNow.Date.AddDays(7)).AddDays(2),
                    Status = status
                };
                db.LeaveRequests.Add(request);
                db.SaveChanges();
                return request;
            }

            private static DateTime NextMonday(DateTime date)
            {
                while (date.DayOfWeek != DayOfWeek.Monday)
                {
                    date = date.AddDays(1);
                }

                return date;
            }

            [Fact]
            public async Task Create_RecordsSubmissionActorAndStatuses()
            {
                using var db = NewInMemoryDb(nameof(Create_RecordsSubmissionActorAndStatuses));
                var (_, employee, _) = SeedOrg(db);
                var before = DateTime.UtcNow;

                var result = await CreateController(db).Create(new CreateLeaveRequestDto
                {
                    EmployeeId = employee.Id,
                    Type = LeaveType.Annual,
                    StartDate = NextMonday(DateTime.UtcNow.Date.AddDays(7)),
                    EndDate = NextMonday(DateTime.UtcNow.Date.AddDays(7)).AddDays(2),
                    Reason = "Test submission"
                });

                Assert.IsType<CreatedAtActionResult>(result.Result);
                var audit = await db.AuditLogs.SingleAsync();
                Assert.Equal(employee.Id, audit.ActorEmployeeId);
                var submittedRequest = await db.LeaveRequests.SingleAsync();
                Assert.Equal(submittedRequest.Id, audit.LeaveRequestId);
                Assert.Equal(AuditLogService.LeaveSubmitted, audit.Action);
                Assert.Equal(LeaveStatus.Draft, audit.PreviousStatus);
                Assert.Equal(LeaveStatus.PendingSupervisorApproval, audit.NewStatus);
                Assert.InRange(audit.OccurredAtUtc, before, DateTime.UtcNow);
            }

            [Theory]
            [InlineData(true, AuditLogService.SupervisorApproved, LeaveStatus.PendingHrApproval)]
            [InlineData(false, AuditLogService.SupervisorRejected, LeaveStatus.Rejected)]
            public async Task SupervisorDecision_RecordsActualActorAndStatuses(
                bool approve,
                string expectedAction,
                LeaveStatus expectedStatus)
            {
                using var db = NewInMemoryDb($"{nameof(SupervisorDecision_RecordsActualActorAndStatuses)}_{approve}");
                var (supervisor, employee, _) = SeedOrg(db);
                var request = AddRequest(db, employee, LeaveStatus.PendingSupervisorApproval);

                var result = await CreateController(db).SupervisorDecision(request.Id, new LeaveDecisionDto
                {
                    DecidedByEmployeeId = supervisor.Id,
                    Approve = approve
                });

                Assert.IsType<OkObjectResult>(result);
                var audit = await db.AuditLogs.SingleAsync();
                Assert.Equal(supervisor.Id, audit.ActorEmployeeId);
                Assert.Equal(request.Id, audit.LeaveRequestId);
                Assert.Equal(expectedAction, audit.Action);
                Assert.Equal(LeaveStatus.PendingSupervisorApproval, audit.PreviousStatus);
                Assert.Equal(expectedStatus, audit.NewStatus);
            }

            [Theory]
            [InlineData(true, AuditLogService.HrApproved, LeaveStatus.Approved)]
            [InlineData(false, AuditLogService.HrRejected, LeaveStatus.Rejected)]
            public async Task HrDecision_RecordsActualActorAndStatuses(
                bool approve,
                string expectedAction,
                LeaveStatus expectedStatus)
            {
                using var db = NewInMemoryDb($"{nameof(HrDecision_RecordsActualActorAndStatuses)}_{approve}");
                var (_, employee, hr) = SeedOrg(db);
                var request = AddRequest(db, employee, LeaveStatus.PendingHrApproval);

                var result = await CreateController(db).HrDecision(request.Id, new LeaveDecisionDto
                {
                    DecidedByEmployeeId = hr.Id,
                    Approve = approve
                });

                Assert.IsType<OkObjectResult>(result);
                var audit = await db.AuditLogs.SingleAsync();
                Assert.Equal(hr.Id, audit.ActorEmployeeId);
                Assert.Equal(request.Id, audit.LeaveRequestId);
                Assert.Equal(expectedAction, audit.Action);
                Assert.Equal(LeaveStatus.PendingHrApproval, audit.PreviousStatus);
                Assert.Equal(expectedStatus, audit.NewStatus);
            }

            [Fact]
            public async Task Recall_RecordsRequestingActorAndStatuses()
            {
                using var db = NewInMemoryDb(nameof(Recall_RecordsRequestingActorAndStatuses));
                var (_, employee, _) = SeedOrg(db);
                var request = AddRequest(db, employee, LeaveStatus.PendingSupervisorApproval);

                var result = await CreateController(db).Recall(request.Id, employee.Id);

                Assert.IsType<OkObjectResult>(result);
                var audit = await db.AuditLogs.SingleAsync();
                Assert.Equal(employee.Id, audit.ActorEmployeeId);
                Assert.Equal(request.Id, audit.LeaveRequestId);
                Assert.Equal(AuditLogService.LeaveRecalled, audit.Action);
                Assert.Equal(LeaveStatus.PendingSupervisorApproval, audit.PreviousStatus);
                Assert.Equal(LeaveStatus.Recalled, audit.NewStatus);
            }

            [Fact]
            public async Task FailedActions_DoNotCreateAuditRecords()
            {
                using var db = NewInMemoryDb(nameof(FailedActions_DoNotCreateAuditRecords));
                var (supervisor, employee, hr) = SeedOrg(db);
                var controller = CreateController(db);
                var supervisorPending = AddRequest(db, employee, LeaveStatus.PendingSupervisorApproval);
                var hrPending = AddRequest(db, employee, LeaveStatus.PendingHrApproval);

                var invalidSubmission = await controller.Create(new CreateLeaveRequestDto
                {
                    EmployeeId = employee.Id,
                    Type = LeaveType.Annual,
                    StartDate = new DateTime(2027, 1, 2),
                    EndDate = new DateTime(2027, 1, 3)
                });
                var unauthorizedSupervisorDecision = await controller.SupervisorDecision(
                    supervisorPending.Id,
                    new LeaveDecisionDto { DecidedByEmployeeId = hr.Id, Approve = true });
                employee.AnnualLeaveBalanceDays = 1;
                var rejectedByBusinessRule = await controller.HrDecision(
                    hrPending.Id,
                    new LeaveDecisionDto { DecidedByEmployeeId = hr.Id, Approve = true });
                var unauthorizedRecall = await controller.Recall(supervisorPending.Id, supervisor.Id);

                Assert.IsType<UnprocessableEntityObjectResult>(invalidSubmission.Result);
                Assert.IsType<ConflictObjectResult>(unauthorizedSupervisorDecision);
                Assert.IsType<ConflictObjectResult>(rejectedByBusinessRule);
                Assert.IsType<ConflictObjectResult>(unauthorizedRecall);
                Assert.Empty(await db.AuditLogs.ToListAsync());
        }

        [Fact]
        public async Task DbSeeder_CreatesAuditTableForExistingDatabase()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            using var db = new ApplicationDbContext(options);
            await db.Database.EnsureCreatedAsync();
            await db.Database.ExecuteSqlRawAsync("DROP TABLE \"AuditLogs\";");

            DbSeeder.Seed(db);

            Assert.Equal(0, await db.AuditLogs.CountAsync());
        }
    }
}
