using LeaveMate.DTOs;
using LeaveMate.Enums;
using LeaveMate.Services;
using LeaveMate.Services.Integration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LeaveMate.Pages.Requests
{
    public class CreateModel : PageModel
    {
        private readonly LeaveMateApiClient _api;

        public CreateModel(LeaveMateApiClient api)
        {
            _api = api;
        }

        [BindProperty]
        public CreateLeaveRequestDto Input { get; set; } = new()
        {
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(1)
        };

        public List<string> Errors { get; private set; } = new();

        public async Task OnGetAsync()
        {
            var currentEmployeeId = HttpContext.Session.GetActiveEmployeeId();
            if (currentEmployeeId is null)
            {
                Response.Redirect("/Account/Login");
                return;
            }

            if (!HttpContext.Session.IsActiveRole("Employee"))
            {
                Response.Redirect("/");
                return;
            }

            Input.EmployeeId = currentEmployeeId.Value;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentEmployeeId = HttpContext.Session.GetActiveEmployeeId();
            if (currentEmployeeId is null)
            {
                return RedirectToPage("/Account/Login");
            }

            if (!HttpContext.Session.IsActiveRole("Employee"))
            {
                return RedirectToPage("/Index");
            }

            Input.EmployeeId = currentEmployeeId.Value;

            if (!ModelState.IsValid)
            {
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Page();
            }

            var (success, errors, _) = await _api.CreateLeaveRequestAsync(Input);
            if (!success)
            {
                Errors = errors;
                return Page();
            }

            return RedirectToPage("/Requests/MyRequests", new { employeeId = Input.EmployeeId });
        }
    }
}
