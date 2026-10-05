using System.Net.Http.Json;
using LeaveMate.DTOs;

namespace LeaveMate.Services.Integration
{
    public record EmployeeSummary(
        int Id,
        string FullName,
        string Email,
        int? SupervisorId,
        bool IsHrAdministrator,
        int AnnualLeaveBalanceDays,
        int SickLeaveBalanceDays,
        int PersonalLeaveBalanceDays);
    public record CoverageResponse(DateTime LastRefreshedUtc, List<CoverageDay> Days);

    /// <summary>
    /// Single integration seam between the Razor Pages front end and the
    /// core business controller layers. Pages never call the DbContext or
    /// services directly — everything goes through this client, keeping the
    /// UI cleanly decoupled from backend internals.
    /// </summary>
    public class LeaveMateApiClient
    {
        private readonly HttpClient _http;

        public LeaveMateApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<EmployeeSummary>> GetEmployeesAsync() =>
            await _http.GetFromJsonAsync<List<EmployeeSummary>>("api/employees") ?? new();

        public async Task<List<LeaveRequestResponseDto>> GetLeaveRequestsAsync(
            int? employeeId = null, string? status = null)
        {
            var query = new List<string>();
            if (employeeId.HasValue) query.Add($"employeeId={employeeId}");
            if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={status}");
            var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";

            return await _http.GetFromJsonAsync<List<LeaveRequestResponseDto>>($"api/leave-requests{qs}") ?? new();
        }

        public async Task<(bool Success, List<string> Errors, LeaveRequestResponseDto? Result)> CreateLeaveRequestAsync(
            CreateLeaveRequestDto dto)
        {
            var response = await _http.PostAsJsonAsync("api/leave-requests", dto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LeaveRequestResponseDto>();
                return (true, new List<string>(), result);
            }

            var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, List<string>>>();
            var errors = problem is not null && problem.TryGetValue("errors", out var list)
                ? list
                : new List<string> { "The request could not be submitted." };

            return (false, errors, null);
        }

        public async Task<bool> SupervisorDecisionAsync(int requestId, LeaveDecisionDto dto)
        {
            var response = await _http.PostAsJsonAsync($"api/leave-requests/{requestId}/supervisor-decision", dto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> HrDecisionAsync(int requestId, LeaveDecisionDto dto)
        {
            var response = await _http.PostAsJsonAsync($"api/leave-requests/{requestId}/hr-decision", dto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RecallAsync(int requestId, int employeeId)
        {
            var response = await _http.PostAsync($"api/leave-requests/{requestId}/recall?employeeId={employeeId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<CoverageResponse?> GetCoverageAsync() =>
            await _http.GetFromJsonAsync<CoverageResponse>("api/coverage");
    }
}
