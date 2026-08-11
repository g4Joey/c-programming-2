using System.Collections.Generic;

namespace LeaveMate.Models
{
    /// <summary>
    /// Minimal employee representation. Full persistence mapping owned by
    /// the Database Administrator / Data Access Engineer tracks.
    /// </summary>
    public class Employee
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Multi-tier routing: every employee (except top-level HR) reports
        // to a supervisor who is the first approval tier.
        public int? SupervisorId { get; set; }
        public Employee? Supervisor { get; set; }

        public bool IsHrAdministrator { get; set; }

        // Remaining leave days per type, used by the validation engine's
        // balance check. Kept simple (Annual balance only) for this slice.
        public int AnnualLeaveBalanceDays { get; set; } = 21;

        public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    }
}
