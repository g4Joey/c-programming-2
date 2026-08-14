using System.Threading.Tasks;

namespace LeaveMate.Services.Notifications
{
    public interface INotificationService
    {
        Task SendAsync(
            string recipient,
            string subject,
            string message);
    }
}