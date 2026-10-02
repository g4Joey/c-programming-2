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

            var hrAdmin = db.Employees.FirstOrDefault(e => e.Email == "ama.boateng@leavemate.local") ?? new Employee
            {
                FullName = "Ama Boateng",
                Email = "ama.boateng@leavemate.local",
                IsHrAdministrator = true,
                AnnualLeaveBalanceDays = 21
            };

            var supervisor = db.Employees.FirstOrDefault(e => e.Email == "kojo.mensah@leavemate.local") ?? new Employee
            {
                FullName = "Kojo Mensah",
                Email = "kojo.mensah@leavemate.local",
                AnnualLeaveBalanceDays = 21
            };

            hrAdmin.IsHrAdministrator = true;
            hrAdmin.SupervisorId = null;
            supervisor.IsHrAdministrator = false;
            supervisor.SupervisorId = null;

            if (hrAdmin.Id == 0) db.Employees.Add(hrAdmin);
            if (supervisor.Id == 0) db.Employees.Add(supervisor);
            db.SaveChanges();

            var jane = db.Employees.FirstOrDefault(e => e.Email == "jane.mensah@leavemate.local") ?? new Employee
            {
                FullName = "Jane Mensah",
                Email = "jane.mensah@leavemate.local",
                AnnualLeaveBalanceDays = 21
            };

            jane.SupervisorId = supervisor.Id;
            jane.IsHrAdministrator = false;
            if (jane.Id == 0) db.Employees.Add(jane);
            db.SaveChanges();

            var manager = db.Employees.First(e => e.Email == "kojo.mensah@leavemate.local");
            var janeEmployee = db.Employees.First(e => e.Email == "jane.mensah@leavemate.local");
            var kendall = db.Employees.FirstOrDefault(e => e.Email == "kendall.brooks@leavemate.local") ?? new Employee
            {
                FullName = "Kendall Brooks",
                Email = "kendall.brooks@leavemate.local",
                SupervisorId = manager.Id,
                AnnualLeaveBalanceDays = 18
            };

            var james = db.Employees.FirstOrDefault(e => e.Email == "james.agyemang@leavemate.local") ?? new Employee
            {
                FullName = "James Osei Agyemang",
                Email = "james.agyemang@leavemate.local",
                SupervisorId = manager.Id,
                AnnualLeaveBalanceDays = 21
            };

            if (kendall.Id == 0) db.Employees.Add(kendall);
            if (james.Id == 0) db.Employees.Add(james);
            db.SaveChanges();

            if (!db.LeaveRequests.Any(r => r.EmployeeId == janeEmployee.Id))
            {
                db.LeaveRequests.Add(new LeaveRequest
                {
                    EmployeeId = janeEmployee.Id,
                    Type = LeaveType.Annual,
                    StartDate = DateTime.UtcNow.Date.AddDays(14),
                    EndDate = DateTime.UtcNow.Date.AddDays(18),
                    Reason = "Family event",
                    Status = LeaveStatus.PendingSupervisorApproval,
                    SubmittedAtUtc = DateTime.UtcNow
                });
            }

            db.SaveChanges();
        }
    }
}
