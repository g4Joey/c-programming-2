using LeaveMate.DTOs;
using LeaveMate.Services.Integration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LeaveMate.Pages.Approvals
{
    public class HrModel : PageModel
    {
        private readonly LeaveMateApiClient _api;

        public HrModel(LeaveMateApiClient api)
        {
            _api = api;
        }

        public int HrAdminId { get; private set; }
        public List<LeaveRequestResponseDto> PendingRequests { get; private set; } = new();

        public async Task OnGetAsync()
        {
            var employees = await _api.GetEmployeesAsync();
            HrAdminId = employees.FirstOrDefault(e => e.IsHrAdministrator)?.Id ?? 0;
            PendingRequests = await _api.GetLeaveRequestsAsync(status: "PendingHrApproval");
        }

        public async Task<IActionResult> OnPostDecideAsync(int requestId, int hrAdminId, bool approve, string? comment)
        {
            await _api.HrDecisionAsync(requestId, new LeaveDecisionDto
            {
                DecidedByEmployeeId = hrAdminId,
                Approve = approve,
                Comment = comment
            });

            return RedirectToPage();
        }
    }
}
