using System.Net.Http.Json;
using System.Text.Json;
using LeaveMate.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace LeaveMate.Web.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IHttpClientFactory httpClientFactory,
        ILogger<HomeController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var requests = new List<LeaveRequestRow>();

        try
        {
            var client = _httpClientFactory.CreateClient("LeaveMateApi");

            var apiRequests =
                await client.GetFromJsonAsync<List<LeaveRequestApiDto>>(
                    "api/leave-requests");

            if (apiRequests != null)
            {
                requests = apiRequests.Select(r => new LeaveRequestRow(
                    FormatLeaveType(r.Type),
                    $"{r.StartDate:MMM dd} – {r.EndDate:MMM dd, yyyy}",
                    r.DurationInDays,
                    MapStatus(r.Status)
                )).ToList();
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Unable to retrieve leave requests from the API.");
            ViewData["ApiError"] =
                "Unable to connect to the leave service. Please try again later.";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "The leave API returned invalid JSON.");
            ViewData["ApiError"] =
                "The leave service returned an unexpected response.";
        }

        return View(requests);
    }

    private static string FormatLeaveType(int type) => type switch
    {
        0 => "Annual Leave",
        1 => "Sick Leave",
        2 => "Personal Leave",
        3 => "Paternity Leave",
        4 => "Unpaid Leave",
        5 => "Compassionate Leave",
        _ => "Leave"
    };

    private static LeaveStatus MapStatus(int status) => status switch
    {
        1 or 2 => LeaveStatus.Pending,
        3 => LeaveStatus.Approved,
        4 => LeaveStatus.Rejected,
        _ => LeaveStatus.Pending
    };

    private sealed class LeaveRequestApiDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public int Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DurationInDays { get; set; }
        public int Status { get; set; }
        public string? Reason { get; set; }
    }
 [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SubmitRequest(
    int Type,
    DateTime StartDate,
    DateTime EndDate,
    string? Reason)
{
    if (StartDate == default || EndDate == default || EndDate < StartDate)
    {
        TempData["ApiError"] =
            "Please enter a valid start date and end date.";
        return RedirectToAction(nameof(Index));
    }

    if (Type < 0 || Type > 5)
    {
        TempData["ApiError"] = "Please select a valid leave type.";
        return RedirectToAction(nameof(Index));
    }

    try
    {
        var client = _httpClientFactory.CreateClient("LeaveMateApi");

        var payload = new
        {
            employeeId = 2,
            type = Type,
            startDate = StartDate,
            endDate = EndDate,
            reason = Reason
        };

        var response = await client.PostAsJsonAsync(
            "api/leave-requests", payload);

        if (response.IsSuccessStatusCode)
        {
            TempData["ApiSuccess"] = "Your leave request was submitted successfully.";
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();

            _logger.LogWarning(
                "Leave request submission failed: {Error}", error);

            TempData["ApiError"] =
                $"The request could not be submitted. {error}";
        }
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Unable to submit leave request.");

        TempData["ApiError"] =
            "Unable to connect to the leave service. Please check that the backend is running.";
    }

    return RedirectToAction(nameof(Index));
}
}