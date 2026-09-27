using LeaveMate.DTOs;
using LeaveMate.Services.Integration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        public List<SelectListItem> EmployeeOptions { get; private set; } = new();
        public List<LeaveRequestResponseDto> Requests { get; private set; } = new();

        public async Task OnGetAsync()
        {
            var employees = await _api.GetEmployeesAsync();
            EmployeeOptions = employees.Select(e => new SelectListItem(e.FullName, e.Id.ToString())).ToList();

            if (EmployeeId == 0 && employees.Any())
            {
                EmployeeId = employees.First().Id;
            }

            Requests = await _api.GetLeaveRequestsAsync(EmployeeId);
        }

        public async Task<IActionResult> OnPostRecallAsync(int requestId, int employeeId)
        {
            await _api.RecallAsync(requestId, employeeId);
            return RedirectToPage(new { employeeId });
        }
    }
}
