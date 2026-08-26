namespace LeaveMate.Web.Models;

public class CalendarEventViewModel
{
    public int Id { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public string LeaveType { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int DurationInDays { get; init; }
    public LeaveStatus Status { get; init; }
}
