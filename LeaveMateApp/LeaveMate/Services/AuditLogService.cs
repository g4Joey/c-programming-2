using LeaveMate.Data;
using LeaveMate.Enums;
using LeaveMate.Models;

namespace LeaveMate.Services
{
    public class AuditLogService : IAuditLogService
    {
        public const string LeaveSubmitted = "LeaveSubmitted";
        public const string SupervisorApproved = "SupervisorApproved";
        public const string SupervisorRejected = "SupervisorRejected";
        public const string HrApproved = "HrApproved";
        public const string HrRejected = "HrRejected";
        public const string LeaveRecalled = "LeaveRecalled";

        private readonly ApplicationDbContext _db;

        public AuditLogService(ApplicationDbContext db)
        {
            _db = db;
        }

        public void Record(
            LeaveRequest request,
            int actorEmployeeId,
            string action,
            LeaveStatus previousStatus,
            LeaveStatus newStatus)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                LeaveRequest = request,
                ActorEmployeeId = actorEmployeeId,
                Action = action,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                OccurredAtUtc = DateTime.UtcNow
            });
        }
    }
}
