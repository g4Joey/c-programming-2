using System.Collections.Generic;

namespace LeaveMate.Models
{
    public class Employee
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int? SupervisorId { get; set; }

        public Employee? Supervisor { get; set; }

        public bool IsHrAdministrator { get; set; }

        public int AnnualLeaveBalanceDays { get; set; } = 21;

        public ICollection<LeaveRequest> LeaveRequests { get; set; }
            = new List<LeaveRequest>();
    }
}
