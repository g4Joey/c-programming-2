using LeaveMate.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LeaveMate.Pages;

public class HelpModel : PageModel
{
    public void OnGet()
    {
        if (HttpContext.Session.GetActiveEmployeeId() is null)
        {
            Response.Redirect("/Account/Login");
        }
    }
}