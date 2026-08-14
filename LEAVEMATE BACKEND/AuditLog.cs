using System;

namespace LeaveMate.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int LeaveRequestId { get; set; }

        public int? PerformedByEmployeeId { get; set; }

        public string Action { get; set; } = string.Empty;

        public string? Details { get; set; }

        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}