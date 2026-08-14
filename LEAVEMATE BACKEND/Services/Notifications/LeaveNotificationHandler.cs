using System.Threading.Tasks;
using LeaveMate.Models;

namespace LeaveMate.Services.Notifications
{
    public class LeaveNotificationHandler
    {
        private readonly INotificationService _notificationService;

        public LeaveNotificationHandler(
            INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task NotifySubmissionAsync(
            LeaveRequest request,
            string recipient)
        {
            await _notificationService.SendAsync(
                recipient,
                "Leave Request Submitted",
                $"Leave request #{request.Id} has been submitted successfully.");
        }

        public async Task NotifyDecisionAsync(
            LeaveRequest request,
            string recipient,
            bool approved,
            string? comment = null)
        {
            var status = approved ? "Approved" : "Rejected";

            var message =
                $"Leave request #{request.Id} has been {status}.";

            if (!string.IsNullOrWhiteSpace(comment))
            {
                message += $" Comment: {comment}";
            }

            await _notificationService.SendAsync(
                recipient,
                $"Leave Request {status}",
                message);
        }

        public async Task NotifyRecallAsync(
            LeaveRequest request,
            string recipient)
        {
            await _notificationService.SendAsync(
                recipient,
                "Leave Request Recalled",
                $"Leave request #{request.Id} has been recalled successfully.");
        }
    }
}