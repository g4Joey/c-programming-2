using LeaveMate.DTOs;
using LeaveMate.Services;
using LeaveMate.Services.Integration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LeaveMate.Pages.Requests
{
    public class MyRequestsModel : PageModel
    {
        private readonly LeaveMateApiClient _api;

        public MyRequestsModel(LeaveMateApiClient api)
        {
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public int EmployeeId { get; set; }

        public List<LeaveRequestResponseDto> Requests { get; private set; } = new();
        public string Heading { get; private set; } = "My Leave Requests";
        public bool CanRecallRequests { get; private set; }
        public bool ShowEmployeeColumn { get; private set; }

        public async Task OnGetAsync()
        {
            var currentEmployeeId = HttpContext.Session.GetActiveEmployeeId();
            if (currentEmployeeId is null)
            {
                Response.Redirect("/Account/Login");
                return;
            }

            var role = HttpContext.Session.GetActiveEmployeeRole();
            if (role == "Employee")
            {
                EmployeeId = currentEmployeeId.Value;
                CanRecallRequests = true;
                Requests = await _api.GetLeaveRequestsAsync(EmployeeId);
                return;
            }

            ShowEmployeeColumn = true;
            var employees = await _api.GetEmployeesAsync();
            var allRequests = await _api.GetLeaveRequestsAsync();

            if (role == "Manager")
            {
                Heading = "Team Leave Requests";
                var reportIds = employees
                    .Where(employee => employee.SupervisorId == currentEmployeeId.Value)
                    .Select(employee => employee.Id)
                    .ToHashSet();
                Requests = allRequests.Where(request => reportIds.Contains(request.EmployeeId)).ToList();
                return;
            }

            if (role == "HR")
            {
                Heading = "All Leave Requests";
                Requests = allRequests;
                return;
            }

            Response.Redirect("/");
        }

        public async Task<IActionResult> OnPostRecallAsync(int requestId, int employeeId)
        {
            var currentEmployeeId = HttpContext.Session.GetActiveEmployeeId();
            if (currentEmployeeId is null)
            {
                return RedirectToPage("/Account/Login");
            }

            if (!HttpContext.Session.IsActiveRole("Employee"))
            {
                return RedirectToPage("/Index");
            }

            await _api.RecallAsync(requestId, currentEmployeeId.Value);
            return RedirectToPage(new { employeeId = currentEmployeeId.Value });
        }
    }
}
