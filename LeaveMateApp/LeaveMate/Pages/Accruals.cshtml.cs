using LeaveMate.Services;
using LeaveMate.Services.Integration;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LeaveMate.Pages;

public class AccrualsModel : PageModel
{
    private readonly LeaveMateApiClient _api;

    public AccrualsModel(LeaveMateApiClient api)
    {
        _api = api;
    }

    public List<EmployeeSummary> Employees { get; private set; } = new();
    public string Heading { get; private set; } = "Accruals";
    public bool ShowEmployeeColumn { get; private set; }

    public async Task OnGetAsync()
    {
        var employeeId = HttpContext.Session.GetActiveEmployeeId();
        if (employeeId is null)
        {
            Response.Redirect("/Account/Login");
            return;
        }

        var employees = await _api.GetEmployeesAsync();
        var role = HttpContext.Session.GetActiveEmployeeRole();

        if (role == "Employee")
        {
            Heading = "My Accruals";
            Employees = employees.Where(employee => employee.Id == employeeId.Value).ToList();
        }
        else if (role == "Manager")
        {
            Heading = "Team Accruals";
            ShowEmployeeColumn = true;
            Employees = employees.Where(employee => employee.SupervisorId == employeeId.Value).ToList();
        }
        else if (role == "HR")
        {
            Heading = "Employee Accruals";
            ShowEmployeeColumn = true;
            Employees = employees;
        }
        else
        {
            Response.Redirect("/");
        }
    }
}