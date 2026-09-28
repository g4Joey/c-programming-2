using LeaveMate.DTOs;
using LeaveMate.Services;
using LeaveMate.Services.Integration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LeaveMate.Pages.Approvals
{
    public class SupervisorModel : PageModel
    {
        private readonly LeaveMateApiClient _api;

        public SupervisorModel(LeaveMateApiClient api)
        {
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public int SupervisorId { get; set; }

        public List<LeaveRequestResponseDto> PendingRequests { get; private set; } = new();

        public async Task OnGetAsync()
        {
            var activeManagerId = HttpContext.Session.GetActiveEmployeeId();
            if (activeManagerId is null || !HttpContext.Session.IsActiveRole("Manager"))
            {
                Response.Redirect("/Account/Login");
                return;
            }

            SupervisorId = activeManagerId.Value;
            var employees = await _api.GetEmployeesAsync();
            var directReportIds = employees.Where(e => e.SupervisorId == SupervisorId).Select(e => e.Id).ToHashSet();
            var pending = await _api.GetLeaveRequestsAsync(status: "PendingSupervisorApproval");
            PendingRequests = pending.Where(r => directReportIds.Contains(r.EmployeeId)).ToList();
        }

        public async Task<IActionResult> OnPostDecideAsync(int requestId, int supervisorId, bool approve, string? comment)
        {
            var activeManagerId = HttpContext.Session.GetActiveEmployeeId();
            if (activeManagerId is null || !HttpContext.Session.IsActiveRole("Manager"))
            {
                return RedirectToPage("/Account/Login");
            }

            await _api.SupervisorDecisionAsync(requestId, new LeaveDecisionDto
            {
                DecidedByEmployeeId = activeManagerId.Value,
                Approve = approve,
                Comment = comment
            });

            return RedirectToPage(new { supervisorId = activeManagerId.Value });
        }
    }
}
