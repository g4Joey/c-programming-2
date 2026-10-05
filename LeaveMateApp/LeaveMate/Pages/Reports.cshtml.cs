using LeaveMate.DTOs;
using LeaveMate.Enums;
using LeaveMate.Services;
using LeaveMate.Services.Integration;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LeaveMate.Pages;

public class ReportsModel : PageModel
{
    private readonly LeaveMateApiClient _api;

    public ReportsModel(LeaveMateApiClient api)
    {
        _api = api;
    }

    public List<LeaveRequestResponseDto> Requests { get; private set; } = new();
    public string Heading { get; private set; } = "Leave Reports";
    public bool ShowEmployeeColumn { get; private set; }
    public int PendingCount => Requests.Count(request => request.Status is LeaveStatus.PendingSupervisorApproval or LeaveStatus.PendingHrApproval);
    public int ApprovedCount => Requests.Count(request => request.Status == LeaveStatus.Approved);
    public int RejectedCount => Requests.Count(request => request.Status == LeaveStatus.Rejected);

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
            Requests = await _api.GetLeaveRequestsAsync(employeeId.Value);
            return;
        }

        ShowEmployeeColumn = true;
        var allRequests = await _api.GetLeaveRequestsAsync();
        if (role == "Manager")
        {
            Heading = "Team Leave Report";
            var employees = await _api.GetEmployeesAsync();
            var reportIds = employees
                .Where(employee => employee.SupervisorId == employeeId.Value)
                .Select(employee => employee.Id)
                .ToHashSet();
            Requests = allRequests.Where(request => reportIds.Contains(request.EmployeeId)).ToList();
        }
        else if (role == "HR")
        {
            Requests = allRequests;
        }
        else
        {
            Response.Redirect("/");
        }
    }
}