using System.ComponentModel.DataAnnotations;
using LeaveMate.Data;
using LeaveMate.Models;
using LeaveMate.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LeaveMate.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public LoginModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Please enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Password = string.Empty;
                return Page();
            }

            var email = Email.Trim().ToLowerInvariant();
            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Email.ToLower() == email);

            // Same message for "unknown email" and "wrong password" so the page
            // doesn't reveal which emails exist.
            if (employee is null || !PasswordMatches(employee, Password))
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                Password = string.Empty;
                return Page();
            }

            var isManager = await _db.Employees.AnyAsync(e => e.SupervisorId == employee.Id);
            var role = GetRole(employee, isManager);

            HttpContext.Session.Clear();
            HttpContext.Session.SetActiveEmployee(employee, role);

            return RedirectToPage("/Index");
        }

        private static bool PasswordMatches(Employee employee, string password)
        {
            if (string.IsNullOrEmpty(employee.PasswordHash))
            {
                return false;
            }

            var hasher = new PasswordHasher<Employee>();
            var result = hasher.VerifyHashedPassword(employee, employee.PasswordHash, password);
            return result != PasswordVerificationResult.Failed;
        }

        private static string GetRole(Employee employee, bool isManager)
        {
            return employee.IsHrAdministrator ? "HR" : isManager ? "Manager" : "Employee";
        }
    }
}