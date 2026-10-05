using LeaveMate.Enums;
using LeaveMate.Services.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LeaveMate.Tests
{
    public class SmtpEmailNotificationServiceTests
    {
        private static LeaveActionNotification SampleNotification() =>
            new(
                "recipient@leavemate.test",
                "Recipient",
                "Requester",
                42,
                LeaveType.Maternity,
                new DateTime(2026, 11, 2),
                new DateTime(2026, 11, 4),
                3,
                "submitted",
                LeaveStatus.PendingSupervisorApproval,
                DateTime.UtcNow,
                "Requester");

        [Fact]
        public async Task SendAsync_WhenDisabled_SkipsDelivery()
        {
            var service = new SmtpEmailNotificationService(
                Options.Create(new EmailOptions()),
                NullLogger<SmtpEmailNotificationService>.Instance);

            var result = await service.SendAsync(SampleNotification());

            Assert.False(result.Sent);
            Assert.True(result.Skipped);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task SendAsync_WhenEnabledWithoutSmtpSettings_ReturnsFailure()
        {
            var service = new SmtpEmailNotificationService(
                Options.Create(new EmailOptions { Enabled = true }),
                NullLogger<SmtpEmailNotificationService>.Instance);

            var result = await service.SendAsync(SampleNotification());

            Assert.False(result.Sent);
            Assert.False(result.Skipped);
            Assert.Equal("Email SMTP settings are incomplete.", result.Error);
        }
    }
}
