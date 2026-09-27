using System.Threading.Tasks;
using LeaveMate.Models;

namespace LeaveMate.Services.Validation
{
    /// <summary>
    /// Context-aware server-side ruleset for leave requests (Key Functionality
    /// #2 of the LeaveMate proposal). Rejects entries starting on weekends,
    /// violating duration thresholds, overlapping existing approved leave,
    /// or exceeding the employee's remaining balance.
    /// </summary>
    public interface ILeaveValidationService
    {
        Task<ValidationResult> ValidateAsync(LeaveRequest request);
    }
}
