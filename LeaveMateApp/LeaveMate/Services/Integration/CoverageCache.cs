namespace LeaveMate.Services.Integration
{
    public record CoverageDay(DateTime Date, int EmployeesOnLeave, IReadOnlyList<string> EmployeeNames);

    /// <summary>Thread-safe holder for the latest computed coverage snapshot.</summary>
    public class CoverageCache
    {
        private readonly object _lock = new();
        private IReadOnlyList<CoverageDay> _days = Array.Empty<CoverageDay>();
        public DateTime LastRefreshedUtc { get; private set; }

        public IReadOnlyList<CoverageDay> GetSnapshot()
        {
            lock (_lock) { return _days; }
        }

        public void Update(IReadOnlyList<CoverageDay> days)
        {
            lock (_lock)
            {
                _days = days;
                LastRefreshedUtc = DateTime.UtcNow;
            }
        }
    }
}
