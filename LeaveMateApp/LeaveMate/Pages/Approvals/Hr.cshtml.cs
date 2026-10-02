using LeaveMate.DTOs;
using LeaveMate.Services;
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
            var activeHrId = HttpContext.Session.GetActiveEmployeeId();
            if (activeHrId is null || !HttpContext.Session.IsActiveRole("HR"))
            {
                Response.Redirect("/Account/Login");
                return;
            }

            HrAdminId = activeHrId.Value;
            PendingRequests = await _api.GetLeaveRequestsAsync(status: "PendingHrApproval");
        }

        public async Task<IActionResult> OnPostDecideAsync(int requestId, int hrAdminId, bool approve, string? comment)
        {
            var activeHrId = HttpContext.Session.GetActiveEmployeeId();
            if (activeHrId is null || !HttpContext.Session.IsActiveRole("HR"))
            {
                return RedirectToPage("/Account/Login");
            }

            await _api.HrDecisionAsync(requestId, new LeaveDecisionDto
            {
                DecidedByEmployeeId = activeHrId.Value,
                Approve = approve,
                Comment = comment
            });

            return RedirectToPage();
        }
    }
}
