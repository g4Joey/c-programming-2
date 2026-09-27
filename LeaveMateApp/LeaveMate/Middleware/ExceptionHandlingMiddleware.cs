using System.Net;
using System.Text.Json;
using LeaveMate.Exceptions;

namespace LeaveMate.Middleware
{
    /// <summary>
    /// Central HTTP exception boundary so every API route returns a
    /// consistent JSON error shape instead of leaking stack traces.
    /// Represents the "exception logic" half of the multi-tier HTTP
    /// routing backend track.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (WorkflowException ex)
            {
                await WriteErrorAsync(context, HttpStatusCode.Conflict, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception processing {Path}", context.Request.Path);
                await WriteErrorAsync(context, HttpStatusCode.InternalServerError,
                    "An unexpected error occurred. Please try again.");
            }
        }

        private static async Task WriteErrorAsync(HttpContext context, HttpStatusCode status, string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)status;
            var payload = JsonSerializer.Serialize(new { error = message });
            await context.Response.WriteAsync(payload);
        }
    }
}
