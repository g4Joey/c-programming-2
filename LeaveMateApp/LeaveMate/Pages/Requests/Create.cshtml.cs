using LeaveMate.DTOs;
using LeaveMate.Enums;
using LeaveMate.Services.Integration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        public List<SelectListItem> EmployeeOptions { get; private set; } = new();
        public List<string> Errors { get; private set; } = new();

        public async Task OnGetAsync()
        {
            await LoadEmployeesAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadEmployeesAsync();

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

        private async Task LoadEmployeesAsync()
        {
            var employees = await _api.GetEmployeesAsync();
            EmployeeOptions = employees
                .Select(e => new SelectListItem(e.FullName, e.Id.ToString()))
                .ToList();
        }
    }
}
