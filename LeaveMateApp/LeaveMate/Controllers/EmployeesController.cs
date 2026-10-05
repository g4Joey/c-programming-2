using LeaveMate.Data;
using LeaveMate.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveMate.Controllers
{
    /// <summary>
    /// Routes employee/org-chart queries needed by the front end (who is a
    /// direct report, who is a supervisor, who is HR) — the multi-tier
    /// routing rules that the approval workflow relies on.
    /// </summary>
    [ApiController]
    [Route("api/employees")]
    public class EmployeesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public EmployeesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAll()
        {
            var employees = await _db.Employees
                .Select(e => new
                {
                    e.Id,
                    e.FullName,
                    e.Email,
                    e.SupervisorId,
                    e.IsHrAdministrator,
                    e.AnnualLeaveBalanceDays,
                    e.SickLeaveBalanceDays,
                    e.PersonalLeaveBalanceDays
                })
                .ToListAsync();

            return Ok(employees);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Employee>> GetById(int id)
        {
            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id);
            return employee is null ? NotFound() : Ok(employee);
        }

        // Direct reports of a supervisor - drives the Supervisor approval queue routing.
        [HttpGet("{id:int}/direct-reports")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetDirectReports(int id)
        {
            var reports = await _db.Employees.Where(e => e.SupervisorId == id).ToListAsync();
            return Ok(reports);
        }
    }
}
