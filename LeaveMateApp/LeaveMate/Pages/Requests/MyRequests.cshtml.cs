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

        public async Task OnGetAsync()
        {
            var currentEmployeeId = HttpContext.Session.GetActiveEmployeeId();
            if (currentEmployeeId is null || !HttpContext.Session.IsActiveRole("Employee"))
            {
                Response.Redirect("/Account/Login");
                return;
            }

            EmployeeId = currentEmployeeId.Value;
            if (EmployeeId > 0)
            {
                Requests = await _api.GetLeaveRequestsAsync(EmployeeId);
            }
        }

        public async Task<IActionResult> OnPostRecallAsync(int requestId, int employeeId)
        {
            var currentEmployeeId = HttpContext.Session.GetActiveEmployeeId();
            if (currentEmployeeId is null || !HttpContext.Session.IsActiveRole("Employee"))
            {
                return RedirectToPage("/Account/Login");
            }

            await _api.RecallAsync(requestId, currentEmployeeId.Value);
            return RedirectToPage(new { employeeId = currentEmployeeId.Value });
        }
    }
}
