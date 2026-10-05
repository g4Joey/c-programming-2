using LeaveMate.Enums;

namespace LeaveMate.Services.Email
{
    public sealed record LeaveActionNotification(
        string RecipientEmail,
        string RecipientName,
        string RequesterName,
        int RequestId,
        LeaveType LeaveType,
        DateTime StartDate,
        DateTime EndDate,
        int DurationInDays,
        string Action,
        LeaveStatus CurrentStatus,
        DateTime ActionTimeUtc,
        string ActorName);
}
