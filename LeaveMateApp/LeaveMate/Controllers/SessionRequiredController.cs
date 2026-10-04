using LeaveMate.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LeaveMate.Web.Controllers;

public abstract class SessionRequiredController : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.HttpContext.Session.GetActiveEmployeeId().HasValue)
        {
            context.Result = new RedirectResult("/Account/Login");
            return;
        }

        base.OnActionExecuting(context);
    }
}
