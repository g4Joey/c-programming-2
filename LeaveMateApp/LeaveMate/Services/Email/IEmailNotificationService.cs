namespace LeaveMate.Services.Email
{
    public interface IEmailNotificationService
    {
        Task<EmailDeliveryResult> SendAsync(
            LeaveActionNotification notification,
            CancellationToken cancellationToken = default);
    }
}
