using System.Threading.Tasks;
using LeaveMate.Models;
using LeaveMate.Services.Notifications;

namespace LEAVEMATE_BACKEND.Tests;

public class UnitTest1
{
    [Fact]
    public async Task NotifySubmissionAsync_UsesEmployeeEmail()
    {
        var fakeService = new FakeNotificationService();
        var handler = new LeaveNotificationHandler(fakeService);

        var request = new LeaveRequest
        {
            Id = 100,
            Employee = new Employee
            {
                Id = 1,
                FullName = "John Doe",
                Email = "employee@leavemate.com"
            }
        };

        await handler.NotifySubmissionAsync(request);

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
    public async Task NotifyDecisionAsync_Approved_UsesEmployeeEmail()
    {
        var fakeService = new FakeNotificationService();
        var handler = new LeaveNotificationHandler(fakeService);

        var request = new LeaveRequest
        {
            Id = 101,
            Employee = new Employee
            {
                Id = 1,
                FullName = "John Doe",
                Email = "employee@leavemate.com"
            }
        };

        await handler.NotifyDecisionAsync(
            request,
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
    public async Task NotifyDecisionAsync_Rejected_UsesEmployeeEmail()
    {
        var fakeService = new FakeNotificationService();
        var handler = new LeaveNotificationHandler(fakeService);

        var request = new LeaveRequest
        {
            Id = 102,
            Employee = new Employee
            {
                Id = 1,
                FullName = "John Doe",
                Email = "employee@leavemate.com"
            }
        };

        await handler.NotifyDecisionAsync(
            request,
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

    [Fact]
    public async Task NotifyRecallAsync_UsesEmployeeEmail()
    {
        var fakeService = new FakeNotificationService();
        var handler = new LeaveNotificationHandler(fakeService);

        var request = new LeaveRequest
        {
            Id = 103,
            Employee = new Employee
            {
                Id = 1,
                FullName = "John Doe",
                Email = "employee@leavemate.com"
            }
        };

        await handler.NotifyRecallAsync(request);

        Assert.Equal(
            "employee@leavemate.com",
            fakeService.Recipient);

        Assert.Equal(
            "Leave Request Recalled",
            fakeService.Subject);

        Assert.Contains(
            "Leave request #103 has been recalled successfully.",
            fakeService.Message);
    }

    [Fact]
    public async Task NotifySubmissionAsync_WithNoEmployeeEmail_DoesNotSendNotification()
    {
        var fakeService = new FakeNotificationService();
        var handler = new LeaveNotificationHandler(fakeService);

        var request = new LeaveRequest
        {
            Id = 104,
            Employee = new Employee
            {
                Id = 1,
                FullName = "John Doe",
                Email = string.Empty
            }
        };

        await handler.NotifySubmissionAsync(request);

        Assert.False(fakeService.WasSent);
        Assert.Null(fakeService.Recipient);
        Assert.Null(fakeService.Subject);
        Assert.Null(fakeService.Message);
    }

    private class FakeNotificationService : INotificationService
    {
        public bool WasSent { get; private set; }

        public string? Recipient { get; private set; }
        public string? Subject { get; private set; }
        public string? Message { get; private set; }

        public Task SendAsync(
            string recipient,
            string subject,
            string message)
        {
            WasSent = true;
            Recipient = recipient;
            Subject = subject;
            Message = message;

            return Task.CompletedTask;
        }
    }
}