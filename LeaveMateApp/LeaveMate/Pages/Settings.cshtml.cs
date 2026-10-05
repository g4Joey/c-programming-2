using LeaveMate.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LeaveMate.Pages;

public class SettingsModel : PageModel
{
    public string UserName { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;

    public void OnGet()
    {
        if (HttpContext.Session.GetActiveEmployeeId() is null)
        {
            Response.Redirect("/Account/Login");
            return;
        }

        UserName = HttpContext.Session.GetActiveEmployeeName() ?? string.Empty;
        Role = HttpContext.Session.GetActiveEmployeeRole() ?? string.Empty;
    }
}