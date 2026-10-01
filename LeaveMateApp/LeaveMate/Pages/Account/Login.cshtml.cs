using LeaveMate.Data;
using LeaveMate.Models;
using LeaveMate.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LeaveMate.Pages.Account
{
    /// <summary>
    /// Demo-only login page: allows selecting any seeded employee without a password.
    /// In production, replace with ASP.NET Identity or an external identity provider.
    /// </summary>
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public LoginModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public int SelectedEmployeeId { get; set; }

        public List<SelectListItem> AccountOptions { get; private set; } = new();

        public async Task OnGetAsync()
        {
            await LoadOptionsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == SelectedEmployeeId);
            if (employee is null)
            {
                await LoadOptionsAsync();
                return Page();
            }

            var isManager = await _db.Employees.AnyAsync(e => e.SupervisorId == employee.Id);
            var role = GetRole(employee, isManager);
            HttpContext.Session.SetActiveEmployee(employee, role);

            return RedirectToPage("/Index");
        }

        private async Task LoadOptionsAsync()
        {
            var employees = await _db.Employees.OrderBy(e => e.FullName).ToListAsync();
            var managerIds = employees
                .Where(e => e.SupervisorId.HasValue)
                .Select(e => e.SupervisorId!.Value)
                .ToHashSet();

            AccountOptions = employees.Select(e => new SelectListItem(
                $"{e.FullName} ({GetRole(e, managerIds.Contains(e.Id))})",
                e.Id.ToString())).ToList();

            if (AccountOptions.Count > 0 && SelectedEmployeeId == 0)
            {
                SelectedEmployeeId = int.Parse(AccountOptions[0].Value);
            }
        }

        private static string GetRole(Employee employee, bool isManager)
        {
            return employee.IsHrAdministrator ? "HR" : isManager ? "Manager" : "Employee";
        }
    }
}
