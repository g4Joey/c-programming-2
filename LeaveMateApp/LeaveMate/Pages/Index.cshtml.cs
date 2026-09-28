using LeaveMate.DTOs;
using LeaveMate.Services;
using LeaveMate.Services.Integration;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LeaveMate.Pages
{
    public class IndexModel : PageModel
    {
        private readonly LeaveMateApiClient _api;

        public IndexModel(LeaveMateApiClient api)
        {
            _api = api;
        }

        public List<LeaveRequestResponseDto> RecentRequests { get; private set; } = new();
        public string CurrentUserName { get; private set; } = "Jane Mensah";
        public string CurrentUserRole { get; private set; } = "Employee";
        public string DashboardHeading { get; private set; } = "My Leave";
        public string DashboardDescription { get; private set; } = string.Empty;
        public string PrimaryActionText { get; private set; } = "Request leave";
        public string PrimaryActionUrl { get; private set; } = "/Requests/Create";

        public async Task OnGetAsync()
        {
            var activeEmployeeId = HttpContext.Session.GetActiveEmployeeId();
            if (activeEmployeeId is null)
            {
                Response.Redirect("/Account/Login");
                return;
            }

            CurrentUserName = HttpContext.Session.GetActiveEmployeeName() ?? CurrentUserName;
            CurrentUserRole = HttpContext.Session.GetActiveEmployeeRole() ?? CurrentUserRole;

            switch (CurrentUserRole)
            {
                case "Employee":
                    DashboardHeading = "My Leave";
                    DashboardDescription = "Track your leave requests and submit time off for approval.";
                    PrimaryActionText = "Request leave";
                    PrimaryActionUrl = "/Requests/Create";
                    RecentRequests = await _api.GetLeaveRequestsAsync(activeEmployeeId);
                    break;
                case "Manager":
                    DashboardHeading = "Team Approvals";
                    DashboardDescription = "Review leave requests submitted by your direct reports.";
                    PrimaryActionText = "Open team queue";
                    PrimaryActionUrl = "/Approvals/Supervisor";
                    var employees = await _api.GetEmployeesAsync();
                    var reportIds = employees
                        .Where(employee => employee.SupervisorId == activeEmployeeId)
                        .Select(employee => employee.Id)
                        .ToHashSet();
                    var managerRequests = await _api.GetLeaveRequestsAsync(status: "PendingSupervisorApproval");
                    RecentRequests = managerRequests.Where(request => reportIds.Contains(request.EmployeeId)).ToList();
                    break;
                case "HR":
                    DashboardHeading = "HR Review";
                    DashboardDescription = "Review requests that have passed manager approval.";
                    PrimaryActionText = "Open HR queue";
                    PrimaryActionUrl = "/Approvals/Hr";
                    RecentRequests = await _api.GetLeaveRequestsAsync(status: "PendingHrApproval");
                    break;
                default:
                    Response.Redirect("/Account/Login");
                    return;
            }

            RecentRequests = RecentRequests
                .OrderByDescending(request => request.SubmittedAtUtc)
                .Take(10)
                .ToList();
        }
    }
}
