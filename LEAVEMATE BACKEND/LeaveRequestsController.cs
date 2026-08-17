using System;
using System.Linq;
using System.Threading.Tasks;
using LeaveMate.Data;
using LeaveMate.DTOs;
using LeaveMate.Exceptions;
using LeaveMate.Models;
using LeaveMate.Services;
using LeaveMate.Services.Notifications;
using LeaveMate.Services.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveMate.Controllers
{
    /// <summary>
    /// Controller logic for leave requests.
    /// Handles validation, workflow decisions, notifications and audit logging.
    /// </summary>
    [ApiController]
    [Route("api/leave-requests")]
    public class LeaveRequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILeaveValidationService _validationService;
        private readonly LeaveWorkflowService _workflowService;
        private readonly LeaveNotificationHandler _notificationHandler;
        private readonly IAuditLogService _auditLogService;

        public LeaveRequestsController(
            ApplicationDbContext db,
            ILeaveValidationService validationService,
            LeaveWorkflowService workflowService,
            LeaveNotificationHandler notificationHandler,
            IAuditLogService auditLogService)
        {
            _db = db;
            _validationService = validationService;
            _workflowService = workflowService;
            _notificationHandler = notificationHandler;
            _auditLogService = auditLogService;
        }

        // ============================================================
        // GET: api/leave-requests
        // ============================================================

        [HttpGet]
        public async Task<ActionResult<object>> GetAll(
            [FromQuery] int? employeeId,
            [FromQuery] string? status)
        {
            var query = _db.LeaveRequests
                .Include(r => r.Employee)
                .AsQueryable();

            if (employeeId.HasValue)
            {
                query = query.Where(
                    r => r.EmployeeId == employeeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status)
                && Enum.TryParse<Enums.LeaveStatus>(
                    status,
                    true,
                    out var parsedStatus))
            {
                query = query.Where(
                    r => r.Status == parsedStatus);
            }

            var results = await query
                .OrderByDescending(r => r.SubmittedAtUtc)
                .Select(r => LeaveRequestResponseDto.FromEntity(r))
                .ToListAsync();

            return Ok(results);
        }

        // ============================================================
        // GET: api/leave-requests/{id}
        // ============================================================

        [HttpGet("{id:int}")]
        public async Task<ActionResult<LeaveRequestResponseDto>> GetById(
            int id)
        {
            var request = await _db.LeaveRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request is null)
            {
                return NotFound();
            }

            return Ok(
                LeaveRequestResponseDto.FromEntity(request));
        }

        // ============================================================
        // POST: api/leave-requests
        // Create / submit leave request
        // ============================================================

        [HttpPost]
        public async Task<ActionResult<LeaveRequestResponseDto>> Create(
            [FromBody] CreateLeaveRequestDto dto)
        {
            var request = new LeaveRequest
            {
                EmployeeId = dto.EmployeeId,
                Type = dto.Type,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Reason = dto.Reason
            };

            var validation =
                await _validationService.ValidateAsync(request);

            if (!validation.IsValid)
            {
                return UnprocessableEntity(
                    new { errors = validation.Errors });
            }

            _workflowService.Submit(request);

            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            try
            {
                _db.LeaveRequests.Add(request);

                // Save the leave request first so the ID is generated.
                await _db.SaveChangesAsync();

                // Add audit record using the same DbContext.
                await _auditLogService.LogAsync(
                    request.Id,
                    request.EmployeeId,
                    "Leave Request Submitted",
                    $"Leave request #{request.Id} was submitted.");

                // Save the audit record inside the same transaction.
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            var saved = await _db.LeaveRequests
                .Include(r => r.Employee)
                .FirstAsync(r => r.Id == request.Id);

            // Recipient is now resolved from the employee's email.
            await _notificationHandler.NotifySubmissionAsync(saved);

            return CreatedAtAction(
                nameof(GetById),
                new { id = request.Id },
                LeaveRequestResponseDto.FromEntity(saved));
        }

        // ============================================================
        // POST: api/leave-requests/{id}/supervisor-decision
        // ============================================================

        [HttpPost("{id:int}/supervisor-decision")]
        public async Task<IActionResult> SupervisorDecision(
            int id,
            [FromBody] LeaveDecisionDto dto)
        {
            var request = await _db.LeaveRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request is null)
            {
                return NotFound();
            }

            try
            {
                await _workflowService.ApplySupervisorDecisionAsync(
                    request,
                    dto.DecidedByEmployeeId,
                    dto.Approve,
                    dto.Comment);
            }
            catch (WorkflowException ex)
            {
                return Conflict(
                    new { error = ex.Message });
            }

            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            try
            {
                var action = dto.Approve
                    ? "Supervisor Approved Leave"
                    : "Supervisor Rejected Leave";

                await _auditLogService.LogAsync(
                    request.Id,
                    dto.DecidedByEmployeeId,
                    action,
                    dto.Comment);

                // Save workflow change and audit record together.
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            // Recipient is resolved from request.Employee.Email.
            await _notificationHandler.NotifyDecisionAsync(
                request,
                dto.Approve,
                dto.Comment);

            return Ok(
                LeaveRequestResponseDto.FromEntity(request));
        }

        // ============================================================
        // POST: api/leave-requests/{id}/hr-decision
        // ============================================================

        [HttpPost("{id:int}/hr-decision")]
        public async Task<IActionResult> HrDecision(
            int id,
            [FromBody] LeaveDecisionDto dto)
        {
            var request = await _db.LeaveRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request is null)
            {
                return NotFound();
            }

            try
            {
                await _workflowService.ApplyHrDecisionAsync(
                    request,
                    dto.DecidedByEmployeeId,
                    dto.Approve,
                    dto.Comment);
            }
            catch (WorkflowException ex)
            {
                return Conflict(
                    new { error = ex.Message });
            }

            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            try
            {
                var action = dto.Approve
                    ? "HR Approved Leave"
                    : "HR Rejected Leave";

                await _auditLogService.LogAsync(
                    request.Id,
                    dto.DecidedByEmployeeId,
                    action,
                    dto.Comment);

                // Save workflow change and audit record together.
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            // Recipient is resolved from request.Employee.Email.
            await _notificationHandler.NotifyDecisionAsync(
                request,
                dto.Approve,
                dto.Comment);

            return Ok(
                LeaveRequestResponseDto.FromEntity(request));
        }

        // ============================================================
        // POST: api/leave-requests/{id}/recall
        // ============================================================

        [HttpPost("{id:int}/recall")]
        public async Task<IActionResult> Recall(
            int id,
            [FromQuery] int employeeId)
        {
            var request = await _db.LeaveRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request is null)
            {
                return NotFound();
            }

            try
            {
                _workflowService.Recall(
                    request,
                    employeeId);
            }
            catch (WorkflowException ex)
            {
                return Conflict(
                    new { error = ex.Message });
            }

            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            try
            {
                await _auditLogService.LogAsync(
                    request.Id,
                    employeeId,
                    "Leave Request Recalled",
                    $"Leave request #{request.Id} was recalled by the employee.");

                // Save workflow change and audit record together.
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            // Recipient is resolved from request.Employee.Email.
            await _notificationHandler.NotifyRecallAsync(request);

            return Ok(
                LeaveRequestResponseDto.FromEntity(request));
        }
    }
}