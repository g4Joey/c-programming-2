using System;
using System.Threading.Tasks;
using LeaveMate.Data;
using LeaveMate.Models;

namespace LeaveMate.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _db;

        public AuditLogService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(
            int leaveRequestId,
            int? performedByEmployeeId,
            string action,
            string? details = null)
        {
            var auditLog = new AuditLog
            {
                LeaveRequestId = leaveRequestId,
                PerformedByEmployeeId = performedByEmployeeId,
                Action = action,
                Details = details,
                TimestampUtc = DateTime.UtcNow
            };

            _db.Set<AuditLog>().Add(auditLog);

            await Task.CompletedTask;
        }
    }
}