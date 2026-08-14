using System.Threading.Tasks;
using LeaveMate.Models;

namespace LeaveMate.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(
            int leaveRequestId,
            int? performedByEmployeeId,
            string action,
            string? details = null);
    }
}