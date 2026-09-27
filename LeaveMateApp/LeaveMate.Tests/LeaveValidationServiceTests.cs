using LeaveMate.Data;
using LeaveMate.Enums;
using LeaveMate.Models;
using LeaveMate.Services.Validation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LeaveMate.Tests
{
    public class LeaveValidationServiceTests
    {
        private static ApplicationDbContext NewInMemoryDb(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(name)
                .Options;
            return new ApplicationDbContext(options);
        }

        private static Employee SeedEmployee(ApplicationDbContext db, int balance = 21)
        {
            var employee = new Employee { FullName = "Test Employee", Email = "t@x.com", AnnualLeaveBalanceDays = balance };
            db.Employees.Add(employee);
            db.SaveChanges();
            return employee;
        }

        private static DateTime NextMonday(DateTime from)
        {
            var date = from.Date;
            while (date.DayOfWeek != DayOfWeek.Monday) date = date.AddDays(1);
            return date;
        }

        [Fact]
        public async Task Rejects_WeekendStart()
        {
            using var db = NewInMemoryDb(nameof(Rejects_WeekendStart));
            var employee = SeedEmployee(db);

            var saturday = DateTime.UtcNow.Date;
            while (saturday.DayOfWeek != DayOfWeek.Saturday) saturday = saturday.AddDays(1);

            var request = new LeaveRequest
            {
                EmployeeId = employee.Id,
                Type = LeaveType.Annual,
                StartDate = saturday,
                EndDate = saturday.AddDays(1)
            };

            var result = await new LeaveValidationService(db).ValidateAsync(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("weekend"));
        }

        [Fact]
        public async Task Rejects_DurationExceedingThreshold()
        {
            using var db = NewInMemoryDb(nameof(Rejects_DurationExceedingThreshold));
            var employee = SeedEmployee(db, balance: 60);
            var start = NextMonday(DateTime.UtcNow.Date.AddDays(1));

            var request = new LeaveRequest
            {
                EmployeeId = employee.Id,
                Type = LeaveType.Annual,
                StartDate = start,
                EndDate = start.AddDays(40) // exceeds 30-day default threshold
            };

            var result = await new LeaveValidationService(db).ValidateAsync(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("exceeds"));
        }

        [Fact]
        public async Task Rejects_OverlappingRequest()
        {
            using var db = NewInMemoryDb(nameof(Rejects_OverlappingRequest));
            var employee = SeedEmployee(db);
            var start = NextMonday(DateTime.UtcNow.Date.AddDays(1));

            db.LeaveRequests.Add(new LeaveRequest
            {
                EmployeeId = employee.Id,
                Type = LeaveType.Annual,
                StartDate = start,
                EndDate = start.AddDays(2),
                Status = LeaveStatus.Approved
            });
            db.SaveChanges();

            var overlapping = new LeaveRequest
            {
                EmployeeId = employee.Id,
                Type = LeaveType.Annual,
                StartDate = start.AddDays(1),
                EndDate = start.AddDays(3)
            };

            var result = await new LeaveValidationService(db).ValidateAsync(overlapping);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("overlap"));
        }

        [Fact]
        public async Task Rejects_InsufficientBalance()
        {
            using var db = NewInMemoryDb(nameof(Rejects_InsufficientBalance));
            var employee = SeedEmployee(db, balance: 2);
            var start = NextMonday(DateTime.UtcNow.Date.AddDays(1));

            var request = new LeaveRequest
            {
                EmployeeId = employee.Id,
                Type = LeaveType.Annual,
                StartDate = start,
                EndDate = start.AddDays(4) // 5 days requested, only 2 available
            };

            var result = await new LeaveValidationService(db).ValidateAsync(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("balance"));
        }

        [Fact]
        public async Task Accepts_ValidRequest()
        {
            using var db = NewInMemoryDb(nameof(Accepts_ValidRequest));
            var employee = SeedEmployee(db, balance: 21);
            var start = NextMonday(DateTime.UtcNow.Date.AddDays(1));

            var request = new LeaveRequest
            {
                EmployeeId = employee.Id,
                Type = LeaveType.Annual,
                StartDate = start,
                EndDate = start.AddDays(2)
            };

            var result = await new LeaveValidationService(db).ValidateAsync(request);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }
    }
}
