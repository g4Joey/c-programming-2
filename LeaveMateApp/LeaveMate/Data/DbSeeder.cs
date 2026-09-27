using LeaveMate.Enums;
using LeaveMate.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaveMate.Data
{
    /// <summary>
    /// Seeds a minimal but realistic dataset so the app is usable on first
    /// run: an HR Administrator, two supervisors, and employees reporting to
    /// them, matching the multi-tier approval model. Owned by the Database
    /// Administrator (table/key design) and Data Access Engineer (EF Core
    /// model wiring) tracks.
    /// </summary>
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext db)
        {
            db.Database.EnsureCreated();

            if (db.Employees.Any())
            {
                return; // Already seeded.
            }

            var hrAdmin = new Employee
            {
                FullName = "Ama Boateng",
                Email = "ama.boateng@leavemate.local",
                IsHrAdministrator = true,
                AnnualLeaveBalanceDays = 21
            };

            var supervisor = new Employee
            {
                FullName = "Kojo Mensah",
                Email = "kojo.mensah@leavemate.local",
                AnnualLeaveBalanceDays = 21
            };

            db.Employees.AddRange(hrAdmin, supervisor);
            db.SaveChanges(); // Persist so identity Ids exist for the FK below.

            var kendall = new Employee
            {
                FullName = "Kendall Brooks",
                Email = "kendall.brooks@leavemate.local",
                SupervisorId = supervisor.Id,
                AnnualLeaveBalanceDays = 18
            };

            var james = new Employee
            {
                FullName = "James Osei Agyemang",
                Email = "james.agyemang@leavemate.local",
                SupervisorId = supervisor.Id,
                AnnualLeaveBalanceDays = 21
            };

            db.Employees.AddRange(kendall, james);
            db.SaveChanges();

            db.LeaveRequests.Add(new LeaveRequest
            {
                EmployeeId = kendall.Id,
                Type = LeaveType.Annual,
                StartDate = DateTime.UtcNow.Date.AddDays(14),
                EndDate = DateTime.UtcNow.Date.AddDays(18),
                Reason = "Family event",
                Status = LeaveStatus.PendingSupervisorApproval,
                SubmittedAtUtc = DateTime.UtcNow
            });

            db.SaveChanges();
        }
    }
}
