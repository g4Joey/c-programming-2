using System.Text.Json;
using LeaveMate.DTOs;
using LeaveMate.Enums;
using LeaveMate.Services;
using LeaveMate.Services.Integration;
using LeaveMate.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace LeaveMate.Web.Controllers;

public class HomeController : SessionRequiredController
{
    private readonly LeaveMateApiClient _api;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        LeaveMateApiClient api,
        ILogger<HomeController> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var requests = new List<LeaveRequestRow>();

        try
        {
            var employeeId = HttpContext.Session.GetActiveEmployeeId();
            if (!employeeId.HasValue)
            {
                return Redirect("/Account/Login");
            }

            var employees = await _api.GetEmployeesAsync();
            var activeEmployee = employees.SingleOrDefault(employee => employee.Id == employeeId.Value);
            if (activeEmployee is null)
            {
                throw new InvalidOperationException(
                    $"The active employee with id {employeeId.Value} could not be found.");
            }

            ViewData["AnnualLeaveBalanceDays"] = activeEmployee.AnnualLeaveBalanceDays;
            ViewData["SickLeaveBalanceDays"] = activeEmployee.SickLeaveBalanceDays;
            ViewData["PersonalLeaveBalanceDays"] = activeEmployee.PersonalLeaveBalanceDays;
            var apiRequests = await _api.GetLeaveRequestsAsync(employeeId);

            requests = apiRequests.Select(request => new LeaveRequestRow(
                request.Type.ToString(),
                $"{request.StartDate:MMM dd} – {request.EndDate:MMM dd, yyyy}",
                request.DurationInDays,
                MapStatus(request.Status)
            )).ToList();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Unable to retrieve leave requests for the portal dashboard.");
            ViewData["ApiError"] =
                "Unable to connect to the leave service. Please try again later.";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "The leave API returned invalid JSON.");
            ViewData["ApiError"] =
                "The leave service returned an unexpected response.";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Unable to retrieve the active employee's leave balance.");
            ViewData["ApiError"] =
                "Unable to retrieve your leave balance right now.";
        }

        ViewData["CurrentUserName"] =
            HttpContext.Session.GetActiveEmployeeName() ?? "Employee";

        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitRequest(
        int Type,
        DateTime StartDate,
        DateTime EndDate,
        string? Reason)
    {
        var employeeId = HttpContext.Session.GetActiveEmployeeId();
        if (!employeeId.HasValue)
        {
            return Redirect("/Account/Login");
        }

        if (StartDate == default || EndDate == default || EndDate < StartDate)
        {
            TempData["ApiError"] = "Please enter a valid start date and end date.";
            return RedirectToAction(nameof(Index));
        }

        if (!Enum.IsDefined(typeof(LeaveType), Type))
        {
            TempData["ApiError"] = "Please select a valid leave type.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var (success, errors, _) = await _api.CreateLeaveRequestAsync(
                new CreateLeaveRequestDto
                {
                    EmployeeId = employeeId.Value,
                    Type = (LeaveType)Type,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    Reason = Reason
                });

            if (success)
            {
                TempData["ApiSuccess"] = "Your leave request was submitted successfully.";
            }
            else
            {
                _logger.LogWarning(
                    "Portal leave request submission failed: {Errors}",
                    string.Join("; ", errors));
                TempData["ApiError"] = string.Join(" ", errors);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Unable to submit a portal leave request.");
            TempData["ApiError"] =
                "Unable to connect to the leave service. Please try again later.";
        }

        return RedirectToAction(nameof(Index));
    }

    private static LeaveMate.Web.Models.LeaveStatus MapStatus(LeaveMate.Enums.LeaveStatus status) =>
        status switch
        {
            LeaveMate.Enums.LeaveStatus.Approved => LeaveMate.Web.Models.LeaveStatus.Approved,
            LeaveMate.Enums.LeaveStatus.Rejected => LeaveMate.Web.Models.LeaveStatus.Rejected,
            _ => LeaveMate.Web.Models.LeaveStatus.Pending
        };
}