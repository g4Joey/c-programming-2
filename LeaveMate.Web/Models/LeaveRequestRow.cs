namespace LeaveMate.Web.Models;

/// <summary>Lightweight row model for the "My Requests" dashboard table.</summary>
public record LeaveRequestRow(string Type, string Dates, int Days, LeaveStatus Status);
