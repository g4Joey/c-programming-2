using LeaveMate.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LeaveMate.Pages
{
    public class CalendarModel : PageModel
    {
        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetActiveEmployeeId() is null)
                return RedirectToPage("/Account/Login");

            return Page();
        }
    }
}
