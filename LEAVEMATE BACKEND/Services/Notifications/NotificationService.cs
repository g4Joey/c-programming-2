using System;
using System.Threading.Tasks;

namespace LeaveMate.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        public async Task SendAsync(
            string recipient,
            string subject,
            string message)
        {
            // Temporary implementation.
            // The actual email/notification provider can be connected later.
            await Task.Run(() =>
            {
                Console.WriteLine("===== LEAVEMATE NOTIFICATION =====");
                Console.WriteLine($"Recipient: {recipient}");
                Console.WriteLine($"Subject: {subject}");
                Console.WriteLine($"Message: {message}");
                Console.WriteLine($"Sent At: {DateTime.UtcNow:O}");
                Console.WriteLine("==================================");
            });
        }
    }
}