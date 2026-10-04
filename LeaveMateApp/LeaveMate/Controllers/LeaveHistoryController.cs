using LeaveMate.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace LeaveMate.Web.Controllers;

public class LeaveHistoryController : SessionRequiredController
{
    public IActionResult Index()
    {
        var model = new LeaveHistoryViewModel
        {
            Requests = new List<LeaveHistoryRowViewModel>
            {
                new() { LeaveType = "Annual", StartDate = new DateTime(2026, 8, 12), EndDate = new DateTime(2026, 8, 16), Days = 5, Status = LeaveStatus.Pending },
                new() { LeaveType = "Sick", StartDate = new DateTime(2026, 5, 3), EndDate = new DateTime(2026, 5, 4), Days = 2, Status = LeaveStatus.Approved },
                new() { LeaveType = "Maternity", StartDate = new DateTime(2026, 9, 7), EndDate = new DateTime(2026, 12, 4), Days = 65, Status = LeaveStatus.Approved },
                new() { LeaveType = "Paternity", StartDate = new DateTime(2026, 6, 15), EndDate = new DateTime(2026, 6, 19), Days = 5, Status = LeaveStatus.Rejected },
                new() { LeaveType = "Unpaid", StartDate = new DateTime(2026, 2, 9), EndDate = new DateTime(2026, 2, 13), Days = 5, Status = LeaveStatus.Rejected },
                new() { LeaveType = "Compassionate", StartDate = new DateTime(2026, 3, 23), EndDate = new DateTime(2026, 3, 24), Days = 2, Status = LeaveStatus.Pending },
            }
        };

        return View(model);
    }
}
