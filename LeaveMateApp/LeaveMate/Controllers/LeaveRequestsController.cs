using System;
using System.Linq;
using System.Threading.Tasks;
using LeaveMate.Data;
using LeaveMate.DTOs;
using LeaveMate.Exceptions;
using LeaveMate.Models;
using LeaveMate.Services;
using LeaveMate.Services.Email;
using LeaveMate.Services.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveMate.Controllers
{
    /// <summary>
    /// Controller logic for the Lead Backend Developer track: wires the
    /// validation engine and workflow service to HTTP endpoints consumed by
    /// the Razor Pages front end.
    /// </summary>
    [ApiController]
    [Route("api/leave-requests")]
    public class LeaveRequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILeaveValidationService _validationService;
        private readonly LeaveWorkflowService _workflowService;
        private readonly IAuditLogService _auditLogService;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly ILogger<LeaveRequestsController> _logger;

        public LeaveRequestsController(
            ApplicationDbContext db,
            ILeaveValidationService validationService,
            LeaveWorkflowService workflowService,
            IAuditLogService auditLogService,
            IEmailNotificationService emailNotificationService,
            ILogger<LeaveRequestsController> logger)
        {
            _db = db;
            _validationService = validationService;
            _workflowService = workflowService;
            _auditLogService = auditLogService;
            _emailNotificationService = emailNotificationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAll(
            [FromQuery] int? employeeId, [FromQuery] string? status)
        {
            var query = _db.LeaveRequests.Include(r => r.Employee).AsQueryable();

            if (employeeId.HasValue)
                query = query.Where(r => r.EmployeeId == employeeId.Value);

            if (!string.IsNullOrWhiteSpace(status)
                && Enum.TryParse<Enums.LeaveStatus>(status, true, out var parsedStatus))
                query = query.Where(r => r.Status == parsedStatus);

            var results = await query
                .OrderByDescending(r => r.SubmittedAtUtc)
                .Select(r => LeaveRequestResponseDto.FromEntity(r))
                .ToListAsync();

            return Ok(results);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<LeaveRequestResponseDto>> GetById(int id)
        {
            var request = await _db.LeaveRequests.Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request is null) return NotFound();
            return Ok(LeaveRequestResponseDto.FromEntity(request));
        }

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

            var validation = await _validationService.ValidateAsync(request);
            if (!validation.IsValid)
            {
                return UnprocessableEntity(new { errors = validation.Errors });
            }

            var previousStatus = request.Status;
            _workflowService.Submit(request);

            _db.LeaveRequests.Add(request);
            _auditLogService.Record(
                request,
                dto.EmployeeId,
                AuditLogService.LeaveSubmitted,
                previousStatus,
                request.Status);
            await _db.SaveChangesAsync();

            var saved = await _db.LeaveRequests
                .Include(r => r.Employee)
                .ThenInclude(employee => employee!.Supervisor)
                .FirstAsync(r => r.Id == request.Id);

            if (saved.Employee?.Supervisor is { } supervisor)
            {
                await SendActionNotificationsAsync(
                    saved,
                    saved.Employee.FullName,
                    "submitted",
                    new[] { supervisor });
            }
            else
            {
                _logger.LogWarning(
                    "No direct supervisor is configured for requester {EmployeeId}; no submission email was sent.",
                    saved.EmployeeId);
            }

            return CreatedAtAction(nameof(GetById), new { id = request.Id },
                LeaveRequestResponseDto.FromEntity(saved));
        }

        [HttpPost("{id:int}/supervisor-decision")]
        public async Task<IActionResult> SupervisorDecision(int id, [FromBody] LeaveDecisionDto dto)
        {
            var request = await _db.LeaveRequests
                .Include(r => r.Employee)
                .ThenInclude(employee => employee!.Supervisor)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request is null) return NotFound();

            var previousStatus = request.Status;
            try
            {
                await _workflowService.ApplySupervisorDecisionAsync(
                    request, dto.DecidedByEmployeeId, dto.Approve, dto.Comment);
            }
            catch (WorkflowException ex)
            {
                return Conflict(new { error = ex.Message });
            }

            _auditLogService.Record(
                request,
                dto.DecidedByEmployeeId,
                dto.Approve
                    ? AuditLogService.SupervisorApproved
                    : AuditLogService.SupervisorRejected,
                previousStatus,
                request.Status);
            await _db.SaveChangesAsync();

            if (dto.Approve)
            {
                var hrRecipients = await _db.Employees
                    .Where(employee => employee.IsHrAdministrator)
                    .ToListAsync();
                await SendActionNotificationsAsync(
                    request,
                    await GetActorNameAsync(dto.DecidedByEmployeeId),
                    "approved by the supervisor and forwarded to HR",
                    hrRecipients);
            }
            else
            {
                await SendActionNotificationsAsync(
                    request,
                    await GetActorNameAsync(dto.DecidedByEmployeeId),
                    "rejected by the supervisor",
                    new[] { request.Employee! });
            }

            return Ok(LeaveRequestResponseDto.FromEntity(request));
        }

        [HttpPost("{id:int}/hr-decision")]
        public async Task<IActionResult> HrDecision(int id, [FromBody] LeaveDecisionDto dto)
        {
            var request = await _db.LeaveRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request is null) return NotFound();

            var previousStatus = request.Status;
            try
            {
                await _workflowService.ApplyHrDecisionAsync(
                    request, dto.DecidedByEmployeeId, dto.Approve, dto.Comment);
            }
            catch (WorkflowException ex)
            {
                return Conflict(new { error = ex.Message });
            }

            _auditLogService.Record(
                request,
                dto.DecidedByEmployeeId,
                dto.Approve
                    ? AuditLogService.HrApproved
                    : AuditLogService.HrRejected,
                previousStatus,
                request.Status);
            await _db.SaveChangesAsync();

            await SendActionNotificationsAsync(
                request,
                await GetActorNameAsync(dto.DecidedByEmployeeId),
                dto.Approve ? "approved by HR" : "rejected by HR",
                new[] { request.Employee! });

            return Ok(LeaveRequestResponseDto.FromEntity(request));
        }

        [HttpPost("{id:int}/recall")]
        public async Task<IActionResult> Recall(int id, [FromQuery] int employeeId)
        {
            var request = await _db.LeaveRequests
                .Include(r => r.Employee)
                .ThenInclude(employee => employee!.Supervisor)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request is null) return NotFound();

            var previousStatus = request.Status;
            try
            {
                _workflowService.Recall(request, employeeId);
            }
            catch (WorkflowException ex)
            {
                return Conflict(new { error = ex.Message });
            }

            _auditLogService.Record(
                request,
                employeeId,
                AuditLogService.LeaveRecalled,
                previousStatus,
                request.Status);
            await _db.SaveChangesAsync();

            var recipients = previousStatus switch
            {
                Enums.LeaveStatus.PendingSupervisorApproval when request.Employee?.Supervisor is { } supervisor
                    => new[] { supervisor },
                Enums.LeaveStatus.PendingHrApproval
                    => await _db.Employees.Where(employee => employee.IsHrAdministrator).ToArrayAsync(),
                _ => Array.Empty<Models.Employee>()
            };
            if (recipients.Length > 0)
            {
                await SendActionNotificationsAsync(
                    request,
                    request.Employee!.FullName,
                    "recalled",
                    recipients);
            }
            else
            {
                _logger.LogWarning(
                    "No current approver is configured for recalled request {RequestId}; no recall email was sent.",
                    request.Id);
            }

            return Ok(LeaveRequestResponseDto.FromEntity(request));
        }

        private async Task<string> GetActorNameAsync(int actorEmployeeId)
        {
            var actorName = await _db.Employees
                .Where(employee => employee.Id == actorEmployeeId)
                .Select(employee => employee.FullName)
                .FirstOrDefaultAsync();

            return actorName
                ?? throw new InvalidOperationException($"Actor employee {actorEmployeeId} was not found.");
        }

        private async Task SendActionNotificationsAsync(
            LeaveRequest request,
            string actorName,
            string action,
            IEnumerable<Models.Employee> recipients)
        {
            if (request.Employee is null)
            {
                throw new InvalidOperationException(
                    $"Requester for leave request {request.Id} was not loaded for email notification.");
            }

            var recipientList = recipients.ToList();
            if (recipientList.Count == 0)
            {
                _logger.LogWarning(
                    "No recipients are configured for the {Action} notification for request {RequestId}.",
                    action,
                    request.Id);
                return;
            }

            var actionTimeUtc = DateTime.UtcNow;
            foreach (var recipient in recipientList)
            {
                var result = await _emailNotificationService.SendAsync(new LeaveActionNotification(
                    recipient.Email,
                    recipient.FullName,
                    request.Employee.FullName,
                    request.Id,
                    request.Type,
                    request.StartDate,
                    request.EndDate,
                    request.DurationInDays,
                    action,
                    request.Status,
                    actionTimeUtc,
                    actorName));

                if (!result.Sent && !result.Skipped)
                {
                    _logger.LogWarning(
                        "Email delivery for leave request {RequestId} failed: {Error}",
                        request.Id,
                        result.Error);
                }
            }
        }
    }
}
