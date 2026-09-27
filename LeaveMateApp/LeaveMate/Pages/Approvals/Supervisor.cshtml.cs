using LeaveMate.DTOs;
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

        public List<SelectListItem> SupervisorOptions { get; private set; } = new();
        public List<LeaveRequestResponseDto> PendingRequests { get; private set; } = new();

        public async Task OnGetAsync()
        {
            var employees = await _api.GetEmployeesAsync();
            var supervisors = employees.Where(e => employees.Any(x => x.SupervisorId == e.Id)).ToList();
            SupervisorOptions = supervisors.Select(e => new SelectListItem(e.FullName, e.Id.ToString())).ToList();

            if (SupervisorId == 0 && supervisors.Any())
            {
                SupervisorId = supervisors.First().Id;
            }

            var directReportIds = employees.Where(e => e.SupervisorId == SupervisorId).Select(e => e.Id).ToHashSet();
            var pending = await _api.GetLeaveRequestsAsync(status: "PendingSupervisorApproval");
            PendingRequests = pending.Where(r => directReportIds.Contains(r.EmployeeId)).ToList();
        }

        public async Task<IActionResult> OnPostDecideAsync(int requestId, int supervisorId, bool approve, string? comment)
        {
            await _api.SupervisorDecisionAsync(requestId, new LeaveDecisionDto
            {
                DecidedByEmployeeId = supervisorId,
                Approve = approve,
                Comment = comment
            });

            return RedirectToPage(new { supervisorId });
        }
    }
}
