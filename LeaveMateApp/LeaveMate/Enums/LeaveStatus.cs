namespace LeaveMate.Enums
{
    /// <summary>
    /// Represents the current position of a leave request
    /// in the approval workflow.
    /// </summary>
    public enum LeaveStatus
    {
        Draft = 0,
        PendingSupervisorApproval = 1,
        PendingHrApproval = 2,
        Approved = 3,
        Rejected = 4,
        Recalled = 5,
        Cancelled = 6
    }
}