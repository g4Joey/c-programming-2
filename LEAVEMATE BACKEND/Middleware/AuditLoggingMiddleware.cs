using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LeaveMate.Middleware
{
    public class AuditLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditLoggingMiddleware> _logger;

        public AuditLoggingMiddleware(
            RequestDelegate next,
            ILogger<AuditLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                var path = context.Request.Path.Value ?? string.Empty;

                if (path.Contains(
                    "/leave-requests/",
                    StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "LeaveMate audit event: Method={Method}, Path={Path}, " +
                        "StatusCode={StatusCode}, TimestampUtc={TimestampUtc}, " +
                        "DurationMs={DurationMs}",
                        context.Request.Method,
                        path,
                        context.Response.StatusCode,
                        DateTime.UtcNow,
                        stopwatch.ElapsedMilliseconds);
                }
            }
        }
    }
}