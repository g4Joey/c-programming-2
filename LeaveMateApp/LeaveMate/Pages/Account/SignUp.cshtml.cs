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
    public class SignUpModel : PageModel
    {
        // New accounts report to this seeded manager so their requests have an approver.
        private const string DefaultSupervisorEmail = "kojo.mensah@leavemate.local";

        private readonly ApplicationDbContext _db;

        public SignUpModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        [Required(ErrorMessage = "Please enter your full name.")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Please choose a password.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Password != ConfirmPassword)
            {
                ModelState.AddModelError(nameof(ConfirmPassword), "Passwords do not match.");
            }

            var email = Email.Trim().ToLowerInvariant();
            if (ModelState.IsValid && await _db.Employees.AnyAsync(e => e.Email.ToLower() == email))
            {
                ModelState.AddModelError(nameof(Email), "An account with this email already exists.");
            }

            if (!ModelState.IsValid)
            {
                Password = string.Empty;
                ConfirmPassword = string.Empty;
                return Page();
            }

            var supervisor = await _db.Employees.FirstOrDefaultAsync(e => e.Email == DefaultSupervisorEmail);
            var employee = new Employee
            {
                FullName = FullName.Trim(),
                Email = email,
                SupervisorId = supervisor?.Id
            };
            employee.PasswordHash = new PasswordHasher<Employee>().HashPassword(employee, Password);

            _db.Employees.Add(employee);
            await _db.SaveChangesAsync();

            HttpContext.Session.Clear();
            HttpContext.Session.SetActiveEmployee(employee, "Employee");

            return RedirectToPage("/Index");
        }
    }
}
