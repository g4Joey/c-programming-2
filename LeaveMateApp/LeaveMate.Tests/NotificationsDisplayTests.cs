using LeaveMate.Enums;
using LeaveMate.Pages;
using Xunit;

namespace LeaveMate.Tests
{
    public class NotificationsDisplayTests
    {
        [Theory]
        [InlineData(LeaveStatus.Draft, "Draft")]
        [InlineData(LeaveStatus.PendingSupervisorApproval, "Pending supervisor approval")]
        [InlineData(LeaveStatus.PendingHrApproval, "Pending HR approval")]
        [InlineData(LeaveStatus.Approved, "Approved")]
        [InlineData(LeaveStatus.Rejected, "Rejected")]
        [InlineData(LeaveStatus.Recalled, "Recalled")]
        [InlineData(LeaveStatus.Cancelled, "Cancelled")]
        public void StatusLabel_ReflectsActualStatus(LeaveStatus status, string expected)
        {
            Assert.Equal(expected, NotificationsModel.GetStatusLabel(status));
        }

        [Theory]
        [InlineData(LeaveStatus.PendingSupervisorApproval, "Awaiting supervisor approval")]
        [InlineData(LeaveStatus.PendingHrApproval, "Awaiting HR approval")]
        [InlineData(LeaveStatus.Approved, "No further approval required")]
        [InlineData(LeaveStatus.Recalled, "No further approval required")]
        [InlineData(LeaveStatus.Cancelled, "No further approval required")]
        public void NextActionLabel_ReflectsWorkflowStatus(LeaveStatus status, string expected)
        {
            Assert.Equal(expected, NotificationsModel.GetNextActionLabel(status));
        }

        [Theory]
        [InlineData(LeaveStatus.PendingSupervisorApproval, "badge--pending")]
        [InlineData(LeaveStatus.PendingHrApproval, "badge--pending")]
        [InlineData(LeaveStatus.Approved, "badge--approved")]
        [InlineData(LeaveStatus.Rejected, "badge--rejected")]
        [InlineData(LeaveStatus.Recalled, "badge--other")]
        [InlineData(LeaveStatus.Cancelled, "badge--other")]
        public void StatusBadgeClass_ReflectsActualStatus(LeaveStatus status, string expected)
        {
            Assert.Equal(expected, NotificationsModel.GetStatusBadgeClass(status));
        }
    }
}
