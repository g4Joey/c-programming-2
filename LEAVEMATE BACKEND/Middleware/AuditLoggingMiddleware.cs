using System.Diagnostics;

namespace LeaveMate.Middleware
{
    public class AuditLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public AuditLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            await _next(context);

            stopwatch.Stop();

            var path = context.Request.Path.Value ?? string.Empty;

            if (path.Contains("/leave-requests/",
                    StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("===== LEAVEMATE AUDIT EVENT =====");
                Console.WriteLine($"Method: {context.Request.Method}");
                Console.WriteLine($"Path: {path}");
                Console.WriteLine($"Status Code: {context.Response.StatusCode}");
                Console.WriteLine($"Timestamp UTC: {DateTime.UtcNow:O}");
                Console.WriteLine($"Duration: {stopwatch.ElapsedMilliseconds} ms");
                Console.WriteLine("=================================");
            }
        }
    }
}