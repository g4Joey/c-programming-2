namespace LeaveMate.Web.Models;

/// <summary>
/// UI-facing view of a leave request's workflow state. Mirrors the semantic
/// status logic from the "Kinetic Enterprise" design system:
/// Pending (amber), Approved (green), Rejected (red).
/// </summary>
public enum LeaveStatus
{
    Pending,
    Approved,
    Rejected
}
