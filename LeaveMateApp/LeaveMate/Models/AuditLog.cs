using System;
using LeaveMate.Enums;

namespace LeaveMate.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int LeaveRequestId { get; set; }
        public int ActorEmployeeId { get; set; }
        public string Action { get; set; } = string.Empty;
        public LeaveStatus PreviousStatus { get; set; }
        public LeaveStatus NewStatus { get; set; }
        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

        public LeaveRequest LeaveRequest { get; set; } = null!;
    }
}
