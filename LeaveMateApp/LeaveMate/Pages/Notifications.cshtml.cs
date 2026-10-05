using LeaveMate.DTOs;
using LeaveMate.Enums;
using LeaveMate.Services;
using LeaveMate.Services.Integration;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LeaveMate.Pages;

public class NotificationsModel : PageModel
{
    private readonly LeaveMateApiClient _api;

    public NotificationsModel(LeaveMateApiClient api)
    {
        _api = api;
    }

    public List<LeaveRequestResponseDto> Items { get; private set; } = new();
    public string Heading { get; private set; } = "Notifications";
    public string QueueUrl { get; private set; } = "/Requests/MyRequests";
    public string QueueLabel { get; private set; } = "View leave requests";

    public async Task OnGetAsync()
    {
        var employeeId = HttpContext.Session.GetActiveEmployeeId();
        if (employeeId is null)
        {
            Response.Redirect("/Account/Login");
            return;
        }

        var role = HttpContext.Session.GetActiveEmployeeRole();
        if (role == "Employee")
        {
            var requests = await _api.GetLeaveRequestsAsync(employeeId.Value);
            Items = requests.Where(IsPending).ToList();
            return;
        }

        if (role == "Manager")
        {
            Heading = "Team Notifications";
            QueueUrl = "/Approvals/Supervisor";
            QueueLabel = "Open team approvals";
            var employees = await _api.GetEmployeesAsync();
            var reportIds = employees
                .Where(employee => employee.SupervisorId == employeeId.Value)
                .Select(employee => employee.Id)
                .ToHashSet();
            var pending = await _api.GetLeaveRequestsAsync(status: "PendingSupervisorApproval");
            Items = pending.Where(request => reportIds.Contains(request.EmployeeId)).ToList();
        }
        else if (role == "HR")
        {
            Heading = "HR Notifications";
            QueueUrl = "/Approvals/Hr";
            QueueLabel = "Open HR review";
            Items = await _api.GetLeaveRequestsAsync(status: "PendingHrApproval");
        }
        else
        {
            Response.Redirect("/");
        }
    }

    private static bool IsPending(LeaveRequestResponseDto request) =>
        request.Status is LeaveStatus.PendingSupervisorApproval or LeaveStatus.PendingHrApproval;

    public static string GetStatusLabel(LeaveStatus status) => status switch
    {
        LeaveStatus.Draft => "Draft",
        LeaveStatus.PendingSupervisorApproval => "Pending supervisor approval",
        LeaveStatus.PendingHrApproval => "Pending HR approval",
        LeaveStatus.Approved => "Approved",
        LeaveStatus.Rejected => "Rejected",
        LeaveStatus.Recalled => "Recalled",
        LeaveStatus.Cancelled => "Cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown leave status.")
    };

    public static string GetNextActionLabel(LeaveStatus status) => status switch
    {
        LeaveStatus.Draft => "Not submitted",
        LeaveStatus.PendingSupervisorApproval => "Awaiting supervisor approval",
        LeaveStatus.PendingHrApproval => "Awaiting HR approval",
        LeaveStatus.Approved or LeaveStatus.Rejected or LeaveStatus.Recalled or LeaveStatus.Cancelled
            => "No further approval required",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown leave status.")
    };

    public static string GetStatusBadgeClass(LeaveStatus status) => status switch
    {
        LeaveStatus.Draft => "badge--other",
        LeaveStatus.PendingSupervisorApproval or LeaveStatus.PendingHrApproval => "badge--pending",
        LeaveStatus.Approved => "badge--approved",
        LeaveStatus.Rejected => "badge--rejected",
        LeaveStatus.Recalled or LeaveStatus.Cancelled => "badge--other",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown leave status.")
    };
}