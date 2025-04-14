namespace CurrencyConverter.Middlewares
{
    using Microsoft.AspNetCore.Http;
    using Serilog;
    using System.Diagnostics;
    using System.Security.Claims;
    using System.Text.Json;

    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            await _next(context);

            stopwatch.Stop();

            var clientIp = context.Connection.RemoteIpAddress?.ToString();
            var clientId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var log = new
            {
                ClientIP = clientIp,
                ClientId = clientId,
                Method = context.Request.Method,
                Path = context.Request.Path,
                StatusCode = context.Response.StatusCode,
                Duration = stopwatch.ElapsedMilliseconds
            };

            Log.Information(JsonSerializer.Serialize(log));
        }
    }
}
