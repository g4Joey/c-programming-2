using LeaveMate.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace LeaveMate.Web.Controllers;

public class CalendarController : SessionRequiredController
{
    public IActionResult Index()
    {
        var events = new List<CalendarEventViewModel>
        {
            new() { Id = 1, EmployeeName = "Obuabang Jessy", LeaveType = "Annual", StartDate = new DateTime(2026, 8, 12), EndDate = new DateTime(2026, 8, 16), DurationInDays = 5, Status = LeaveStatus.Pending },
            new() { Id = 2, EmployeeName = "Jessica Ashorkor", LeaveType = "Sick", StartDate = new DateTime(2026, 8, 18), EndDate = new DateTime(2026, 8, 19), DurationInDays = 2, Status = LeaveStatus.Approved },
            new() { Id = 3, EmployeeName = "Mohammed Iddrisu", LeaveType = "Maternity", StartDate = new DateTime(2026, 8, 24), EndDate = new DateTime(2026, 9, 4), DurationInDays = 10, Status = LeaveStatus.Approved },
            new() { Id = 4, EmployeeName = "Obuabang Jessy", LeaveType = "Compassionate", StartDate = new DateTime(2026, 8, 28), EndDate = new DateTime(2026, 8, 31), DurationInDays = 4, Status = LeaveStatus.Rejected },
            new() { Id = 5, EmployeeName = "Jessica Ashorkor", LeaveType = "Paternity", StartDate = new DateTime(2026, 7, 30), EndDate = new DateTime(2026, 8, 3), DurationInDays = 5, Status = LeaveStatus.Pending },
        };

        return View(events);
    }
}
