namespace LeaveMate.Web.Models;

public class ApprovalQueueItemViewModel
{
    public int Id { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public string LeaveType { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int Days { get; init; }
    public string Reason { get; init; } = string.Empty;
    public LeaveStatus Status { get; init; } = LeaveStatus.Pending;
}
