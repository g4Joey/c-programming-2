using LeaveMate.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace LeaveMate.Web.Controllers;

public class ApprovalsController : SessionRequiredController
{
    public IActionResult Index()
    { 
        var queue = new List<ApprovalQueueItemViewModel>
        {
            new() { Id = 101, EmployeeName = "Obuabang Jessy", LeaveType = "Annual", StartDate = new DateTime(2026, 8, 26), EndDate = new DateTime(2026, 8, 28), Days = 3, Reason = "Family holiday", Status = LeaveStatus.Pending },
            new() { Id = 102, EmployeeName = "Mohammed Iddrisu", LeaveType = "Sick", StartDate = new DateTime(2026, 9, 2), EndDate = new DateTime(2026, 9, 3), Days = 2, Reason = "Medical appointment and recovery", Status = LeaveStatus.Pending },
            new() { Id = 103, EmployeeName = "Ashorkor Jessica", LeaveType = "Unpaid", StartDate = new DateTime(2026, 10, 12), EndDate = new DateTime(2026, 10, 16), Days = 5, Reason = "Personal commitments", Status = LeaveStatus.Pending },
        };

        return View(queue);
    }
}
