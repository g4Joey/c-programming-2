using LeaveMate.DTOs;
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

        public async Task OnGetAsync()
        {
            var all = await _api.GetLeaveRequestsAsync();
            RecentRequests = all.Take(10).ToList();
        }
    }
}
