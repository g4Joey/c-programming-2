using System;
using System.Linq;
using System.Threading.Tasks;
using LeaveMate.Data;
using LeaveMate.DTOs;
using LeaveMate.Exceptions;
using LeaveMate.Models;
using LeaveMate.Services;
using LeaveMate.Services.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveMate.Controllers
{
    /// <summary>
    /// Controller logic for the Lead Backend Developer track: wires the
    /// validation engine and workflow service to HTTP endpoints consumed by
    /// the Razor/Blazor front end.
    /// </summary>
    [ApiController]
    [Route("api/leave-requests")]
    public class LeaveRequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILeaveValidationService _validationService;
        private readonly LeaveWorkflowService _workflowService;

        public LeaveRequestsController(
            ApplicationDbContext db,
            ILeaveValidationService validationService,
            LeaveWorkflowService workflowService)
        {
            _db = db;
            _validationService = validationService;
            _workflowService = workflowService;
        }

        // GET api/leave-requests?employeeId=6&status=PendingSupervisorApproval
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

        // GET api/leave-requests/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<LeaveRequestResponseDto>> GetById(int id)
        {
            var request = await _db.LeaveRequests.Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request is null) return NotFound();
            return Ok(LeaveRequestResponseDto.FromEntity(request));
        }

        // POST api/leave-requests
        // Runs the Context-Aware Validation Engine before persisting anything.
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

            _workflowService.Submit(request);

            _db.LeaveRequests.Add(request);
            await _db.SaveChangesAsync();

            var saved = await _db.LeaveRequests.Include(r => r.Employee)
                .FirstAsync(r => r.Id == request.Id);

            return CreatedAtAction(nameof(GetById), new { id = request.Id },
                LeaveRequestResponseDto.FromEntity(saved));
        }

        // POST api/leave-requests/5/supervisor-decision
        [HttpPost("{id:int}/supervisor-decision")]
        public async Task<IActionResult> SupervisorDecision(int id, [FromBody] LeaveDecisionDto dto)
        {
            var request = await _db.LeaveRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request is null) return NotFound();

            try
            {
                await _workflowService.ApplySupervisorDecisionAsync(
                    request, dto.DecidedByEmployeeId, dto.Approve, dto.Comment);
            }
            catch (WorkflowException ex)
            {
                return Conflict(new { error = ex.Message });
            }

            await _db.SaveChangesAsync();
            return Ok(LeaveRequestResponseDto.FromEntity(request));
        }

        // POST api/leave-requests/5/hr-decision
        [HttpPost("{id:int}/hr-decision")]
        public async Task<IActionResult> HrDecision(int id, [FromBody] LeaveDecisionDto dto)
        {
            var request = await _db.LeaveRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request is null) return NotFound();

            try
            {
                await _workflowService.ApplyHrDecisionAsync(
                    request, dto.DecidedByEmployeeId, dto.Approve, dto.Comment);
            }
            catch (WorkflowException ex)
            {
                return Conflict(new { error = ex.Message });
            }

            await _db.SaveChangesAsync();
            return Ok(LeaveRequestResponseDto.FromEntity(request));
        }

        // POST api/leave-requests/5/recall
        [HttpPost("{id:int}/recall")]
        public async Task<IActionResult> Recall(int id, [FromQuery] int employeeId)
        {
            var request = await _db.LeaveRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request is null) return NotFound();

            try
            {
                _workflowService.Recall(request, employeeId);
            }
            catch (WorkflowException ex)
            {
                return Conflict(new { error = ex.Message });
            }

            await _db.SaveChangesAsync();
            return Ok(LeaveRequestResponseDto.FromEntity(request));
        }
    }
}
