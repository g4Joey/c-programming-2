using System.Threading.Tasks;
using LeaveMate.Models;
using LeaveMate.Services.Notifications;

namespace LEAVEMATE_BACKEND.Tests;

public class UnitTest1
{
    [Fact]
    public async Task NotifySubmissionAsync_SendsSubmissionNotification()
    {
        var fakeService = new FakeNotificationService();
        var handler = new LeaveNotificationHandler(fakeService);

        var request = new LeaveRequest
        {
            Id = 100
        };

        await handler.NotifySubmissionAsync(
            request,
            "employee@leavemate.com");

        Assert.Equal(
            "employee@leavemate.com",
            fakeService.Recipient);

        Assert.Equal(
            "Leave Request Submitted",
            fakeService.Subject);

        Assert.Contains(
            "Leave request #100 has been submitted successfully.",
            fakeService.Message);
    }

    [Fact]
    public async Task NotifyDecisionAsync_Approved_SendsApprovalNotification()
    {
        var fakeService = new FakeNotificationService();
        var handler = new LeaveNotificationHandler(fakeService);

        var request = new LeaveRequest
        {
            Id = 101
        };

        await handler.NotifyDecisionAsync(
            request,
            "employee@leavemate.com",
            true,
            "Approved by supervisor");

        Assert.Equal(
            "employee@leavemate.com",
            fakeService.Recipient);

        Assert.Equal(
            "Leave Request Approved",
            fakeService.Subject);

        Assert.Contains(
            "Leave request #101 has been Approved.",
            fakeService.Message);

        Assert.Contains(
            "Approved by supervisor",
            fakeService.Message);
    }

    [Fact]
    public async Task NotifyDecisionAsync_Rejected_SendsRejectionNotification()
    {
        var fakeService = new FakeNotificationService();
        var handler = new LeaveNotificationHandler(fakeService);

        var request = new LeaveRequest
        {
            Id = 102
        };

        await handler.NotifyDecisionAsync(
            request,
            "employee@leavemate.com",
            false,
            "Insufficient leave balance");

        Assert.Equal(
            "employee@leavemate.com",
            fakeService.Recipient);

        Assert.Equal(
            "Leave Request Rejected",
            fakeService.Subject);

        Assert.Contains(
            "Leave request #102 has been Rejected.",
            fakeService.Message);

        Assert.Contains(
            "Insufficient leave balance",
            fakeService.Message);
    }

    private class FakeNotificationService : INotificationService
    {
        public string? Recipient { get; private set; }
        public string? Subject { get; private set; }
        public string? Message { get; private set; }

        public Task SendAsync(
            string recipient,
            string subject,
            string message)
        {
            Recipient = recipient;
            Subject = subject;
            Message = message;

            return Task.CompletedTask;
        }
    }
}