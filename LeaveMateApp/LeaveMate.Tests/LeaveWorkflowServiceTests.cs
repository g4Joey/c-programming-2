using LeaveMate.Data;
using LeaveMate.Enums;
using LeaveMate.Exceptions;
using LeaveMate.Models;
using LeaveMate.Services;
using Microsoft.EntityFrameworkCore;
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
}
