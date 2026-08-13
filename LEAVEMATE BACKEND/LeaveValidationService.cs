using System;
using System.Linq;
using System.Threading.Tasks;
using LeaveMate.Data;
using LeaveMate.Enums;
using LeaveMate.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaveMate.Services.Validation
{
    public class LeaveValidationService : ILeaveValidationService
    {
        private readonly ApplicationDbContext _db;

        // Business rule thresholds. In a fuller build these would come from
        // configuration / a policy table owned by HR Admin, kept as
        // constants here to keep the engine's intent explicit.
        private const int MinDurationDays = 1;
        private const int MaxDurationDaysDefault = 30;
        private const int MaxUnpaidDurationDays = 90;

        public LeaveValidationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ValidationResult> ValidateAsync(LeaveRequest request)
        {
            var result = ValidationResult.Success();

            var employee = await _db.Employees
                .Include(e => e.LeaveRequests)
                .FirstOrDefaultAsync(e => e.Id == request.EmployeeId);

            if (employee is null)
            {
                result.AddError($"Employee with id {request.EmployeeId} was not found.");
                return result; // No further rule can be evaluated without the employee.
            }

            ValidateDateOrdering(request, result);
            ValidateNotWeekendStart(request, result);
            ValidateDurationThreshold(request, result);

            if (result.IsValid)
            {
                // These checks depend on a structurally sound date range,
                // so only run them once the basics pass.
                await ValidateNoOverlapAsync(request, result);
                ValidateSufficientBalance(request, employee, result);
            }

            return result;
        }

        private static void ValidateDateOrdering(LeaveRequest request, ValidationResult result)
        {
            if (request.EndDate.Date < request.StartDate.Date)
            {
                result.AddError("End date cannot be earlier than start date.");
            }

            if (request.StartDate.Date < DateTime.UtcNow.Date)
            {
                result.AddError("Leave cannot be requested for a date in the past.");
            }
        }

        private static void ValidateNotWeekendStart(LeaveRequest request, ValidationResult result)
        {
            var startDay = request.StartDate.DayOfWeek;
            if (startDay is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                result.AddError(
                    $"Leave cannot start on a weekend ({startDay}). Choose the next working day.");
            }
        }

        private static void ValidateDurationThreshold(LeaveRequest request, ValidationResult result)
        {
            var duration = request.DurationInDays;

            if (duration < MinDurationDays)
            {
                result.AddError("Leave duration must be at least one day.");
                return;
            }

            var maxAllowed = request.Type == LeaveType.Unpaid
                ? MaxUnpaidDurationDays
                : MaxDurationDaysDefault;

            if (duration > maxAllowed)
            {
                result.AddError(
                    $"Requested duration of {duration} day(s) exceeds the {maxAllowed}-day limit for {request.Type} leave.");
            }
        }

        private async Task ValidateNoOverlapAsync(LeaveRequest request, ValidationResult result)
        {
            var hasOverlap = await _db.LeaveRequests
                .Where(r => r.EmployeeId == request.EmployeeId
                            && r.Id != request.Id
                            && r.Status != LeaveStatus.Rejected
                            && r.Status != LeaveStatus.Cancelled
                            && r.Status != LeaveStatus.Recalled)
                .AnyAsync(r => request.StartDate.Date <= r.EndDate.Date
                               && request.EndDate.Date >= r.StartDate.Date);

            if (hasOverlap)
            {
                result.AddError("Requested dates overlap with an existing leave request for this employee.");
            }
        }

        private static void ValidateSufficientBalance(
            LeaveRequest request, Employee employee, ValidationResult result)
        {
            if (request.Type != LeaveType.Annual)
            {
                return; // Balance tracking in this slice covers annual leave only.
            }

            if (request.DurationInDays > employee.AnnualLeaveBalanceDays)
            {
                result.AddError(
                    $"Requested {request.DurationInDays} day(s) exceed the employee's remaining " +
                    $"annual leave balance of {employee.AnnualLeaveBalanceDays} day(s).");
            }
        }
    }
}
