namespace LeaveMate.Web.Models;

public class LeaveHistoryViewModel
{
    public IReadOnlyList<LeaveHistoryRowViewModel> Requests { get; init; } = Array.Empty<LeaveHistoryRowViewModel>();
}

public class LeaveHistoryRowViewModel
{
    public string LeaveType { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int Days { get; init; }
    public LeaveStatus Status { get; init; }
}
