using LeaveMate.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace LeaveMate.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // Sample data so the dashboard shell and components render with realistic content.
        var requests = new List<LeaveRequestRow>
        {
            new("Annual Leave", "Aug 12 – Aug 16, 2026", 5, LeaveStatus.Pending),
            new("Sick Leave", "May 03 – May 04, 2026", 2, LeaveStatus.Approved),
            new("Annual Leave", "Dec 24 – Dec 31, 2025", 6, LeaveStatus.Rejected),
        };

        return View(requests);
    }

    public IActionResult Error() => Problem();
}
