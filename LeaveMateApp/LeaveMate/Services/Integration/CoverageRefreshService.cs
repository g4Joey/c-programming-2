using LeaveMate.Data;
using LeaveMate.Enums;
using Microsoft.EntityFrameworkCore;

namespace LeaveMate.Services.Integration
{
    /// <summary>
    /// Recomputes the 60-day coverage matrix every 30 seconds in the
    /// background, so the calendar view never blocks a request on a live
    /// aggregate query. Represents the "background asynchronous request
    /// handlers" half of the backend track.
    /// </summary>
    public class CoverageRefreshService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly CoverageCache _cache;
        private readonly ILogger<CoverageRefreshService> _logger;

        public CoverageRefreshService(
            IServiceScopeFactory scopeFactory, CoverageCache cache, ILogger<CoverageRefreshService> logger)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Coverage matrix refresh failed.");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task RefreshAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var today = DateTime.UtcNow.Date;
            var horizon = today.AddDays(60);

            var approved = await db.LeaveRequests
                .Include(r => r.Employee)
                .Where(r => r.Status == LeaveStatus.Approved
                            && r.EndDate.Date >= today && r.StartDate.Date <= horizon)
                .ToListAsync(ct);

            var days = new List<CoverageDay>();
            for (var day = today; day <= horizon; day = day.AddDays(1))
            {
                var onLeave = approved
                    .Where(r => day >= r.StartDate.Date && day <= r.EndDate.Date)
                    .Select(r => r.Employee?.FullName ?? "Unknown")
                    .ToList();

                days.Add(new CoverageDay(day, onLeave.Count, onLeave));
            }

            _cache.Update(days);
        }
    }
}
