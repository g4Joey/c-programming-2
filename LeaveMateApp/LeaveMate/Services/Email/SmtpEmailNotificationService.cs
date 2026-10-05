using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Authentication;
using Microsoft.Extensions.Options;

namespace LeaveMate.Services.Email
{
    public sealed class SmtpEmailNotificationService : IEmailNotificationService
    {
        private readonly EmailOptions _options;
        private readonly ILogger<SmtpEmailNotificationService> _logger;

        public SmtpEmailNotificationService(
            IOptions<EmailOptions> options,
            ILogger<SmtpEmailNotificationService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<EmailDeliveryResult> SendAsync(
            LeaveActionNotification notification,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Email notifications are disabled.");
                return EmailDeliveryResult.Skip();
            }

            if (string.IsNullOrWhiteSpace(_options.SmtpHost)
                || _options.SmtpPort is < 1 or > 65535
                || string.IsNullOrWhiteSpace(_options.FromAddress)
                || (!string.IsNullOrWhiteSpace(_options.Username)
                    && string.IsNullOrWhiteSpace(_options.Password)))
            {
                const string error = "Email SMTP settings are incomplete.";
                _logger.LogError("{Error}", error);
                return EmailDeliveryResult.Failure(error);
            }

            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(_options.FromAddress, _options.FromName),
                    Subject = $"Leave request #{notification.RequestId}: {notification.Action}",
                    Body = BuildBody(notification),
                    IsBodyHtml = false
                };
                message.To.Add(new MailAddress(notification.RecipientEmail, notification.RecipientName));

                using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
                {
                    EnableSsl = _options.UseSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 15000
                };

                if (!string.IsNullOrWhiteSpace(_options.Username))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_options.Username, _options.Password);
                }

                await client.SendMailAsync(message, cancellationToken);
                _logger.LogInformation(
                    "Sent leave action email for request {RequestId} to {RecipientEmail}.",
                    notification.RequestId,
                    notification.RecipientEmail);
                return EmailDeliveryResult.Success();
            }
            catch (Exception ex) when (
                ex is SmtpException
                or IOException
                or SocketException
                or AuthenticationException
                or FormatException
                or ArgumentException
                or InvalidOperationException
                or OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Failed to send leave action email for request {RequestId} to {RecipientEmail}.",
                    notification.RequestId,
                    notification.RecipientEmail);
                return EmailDeliveryResult.Failure(ex.Message);
            }
        }

        private static string BuildBody(LeaveActionNotification notification) =>
            string.Join(
                Environment.NewLine,
                $"Hello {notification.RecipientName},",
                string.Empty,
                $"Leave request #{notification.RequestId} has been {notification.Action}.",
                $"Requester: {notification.RequesterName}",
                $"Leave type: {notification.LeaveType}",
                $"Dates: {notification.StartDate.ToString("ddd, MMM d, yyyy", CultureInfo.InvariantCulture)} - {notification.EndDate.ToString("ddd, MMM d, yyyy", CultureInfo.InvariantCulture)}",
                $"Duration: {notification.DurationInDays} day(s)",
                $"Action: {notification.Action} by {notification.ActorName}",
                $"Current status: {notification.CurrentStatus}",
                $"Action time (UTC): {notification.ActionTimeUtc.ToString("O", CultureInfo.InvariantCulture)}",
                string.Empty,
                "This is an automated LeaveMate notification.");
    }
}
