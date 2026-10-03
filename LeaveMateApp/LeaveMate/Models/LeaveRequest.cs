using System;
using LeaveMate.Enums;

namespace LeaveMate.Models
{
    /// <summary>
    /// Core domain entity for a leave submission. Owned by the Lead Backend
    /// Developer track: state transitions here are what the validation
    /// engine and controller logic protect.
    /// </summary>
    public class LeaveRequest
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public LeaveType Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string? Reason { get; set; }

        public LeaveStatus Status { get; set; } = LeaveStatus.Draft;

        public int? LastActionedByEmployeeId { get; set; }
        public string? SupervisorComment { get; set; }
        public string? HrComment { get; set; }

        public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? DecidedAtUtc { get; set; }

        /// <summary>Inclusive working-day span requested.</summary>
        public int DurationInDays => (EndDate.Date - StartDate.Date).Days + 1;
    }
}
