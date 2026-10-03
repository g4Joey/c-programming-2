using System;
using System.Threading.Tasks;
using LeaveMate.Data;
using LeaveMate.Enums;
using LeaveMate.Exceptions;
using LeaveMate.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaveMate.Services
{
    /// <summary>
    /// Drives the request through its multi-tier states:
    /// Draft -> PendingSupervisorApproval -> PendingHrApproval -> Approved/Rejected.
    /// Rejection or recall at any tier ends the workflow.
    /// </summary>
    public class LeaveWorkflowService
    {
        private readonly ApplicationDbContext _db;

        public LeaveWorkflowService(ApplicationDbContext db)
        {
            _db = db;
        }

        public void Submit(LeaveRequest request)
        {
            request.Status = LeaveStatus.PendingSupervisorApproval;
            request.SubmittedAtUtc = DateTime.UtcNow;
        }

        public async Task ApplySupervisorDecisionAsync(
            LeaveRequest request, int decidedByEmployeeId, bool approve, string? comment)
        {
            EnsureStatus(request, LeaveStatus.PendingSupervisorApproval);
            await EnsureIsSupervisorOfAsync(decidedByEmployeeId, request.EmployeeId);

            request.SupervisorComment = comment;
            request.LastActionedByEmployeeId = decidedByEmployeeId;

            if (approve)
            {
                request.Status = LeaveStatus.PendingHrApproval;
            }
            else
            {
                request.Status = LeaveStatus.Rejected;
                request.DecidedAtUtc = DateTime.UtcNow;
            }
        }

        public async Task ApplyHrDecisionAsync(
            LeaveRequest request, int decidedByEmployeeId, bool approve, string? comment)
        {
            EnsureStatus(request, LeaveStatus.PendingHrApproval);
            await EnsureIsHrAdminAsync(decidedByEmployeeId);

            request.HrComment = comment;
            request.LastActionedByEmployeeId = decidedByEmployeeId;
            request.Status = approve ? LeaveStatus.Approved : LeaveStatus.Rejected;
            request.DecidedAtUtc = DateTime.UtcNow;
        }

        public void Recall(LeaveRequest request, int requestedByEmployeeId)
        {
            if (request.EmployeeId != requestedByEmployeeId)
            {
                throw new WorkflowException("Only the requesting employee can recall their own leave request.");
            }

            if (request.Status is LeaveStatus.Approved or LeaveStatus.Rejected or LeaveStatus.Cancelled)
            {
                throw new WorkflowException($"A request in '{request.Status}' status can no longer be recalled.");
            }

            request.Status = LeaveStatus.Recalled;
            request.DecidedAtUtc = DateTime.UtcNow;
        }

        private static void EnsureStatus(LeaveRequest request, LeaveStatus expected)
        {
            if (request.Status != expected)
            {
                throw new WorkflowException(
                    $"Request is in '{request.Status}' status; expected '{expected}' for this action.");
            }
        }

        private async Task EnsureIsSupervisorOfAsync(int supervisorId, int employeeId)
        {
            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
            if (employee?.SupervisorId != supervisorId)
            {
                throw new WorkflowException("Only the employee's direct supervisor may action this tier.");
            }
        }

       private async Task EnsureIsHrAdminAsync(int employeeId)
{
    var isHr = await _db.Employees.AnyAsync(
        e => e.Id == employeeId && e.IsHrAdministrator);

    if (!isHr)
    {
        throw new WorkflowException(
            "Only an HR Administrator may action this tier.");
    }
}

    }
}
