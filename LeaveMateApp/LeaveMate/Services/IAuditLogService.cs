using LeaveMate.Enums;
using LeaveMate.Models;

namespace LeaveMate.Services
{
    public interface IAuditLogService
    {
        void Record(
            LeaveRequest request,
            int actorEmployeeId,
            string action,
            LeaveStatus previousStatus,
            LeaveStatus newStatus);
    }
}
