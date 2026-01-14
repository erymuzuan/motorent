using MotoRent.Domain.Core;
using MotoRent.Services.Core;

namespace MotoRent.Server.Middleware;

/// <summary>
/// Middleware that captures unhandled exceptions and logs them to the database.
/// Supports both web page errors and API errors with appropriate EventLog categorization.
/// </summary>
public class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate m_next;
    private readonly ILogger<ExceptionLoggingMiddleware> m_logger;

    public ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger)
    {
        m_next = next;
        m_logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, LogEntryService logService)
    {
        try
        {
            await m_next(context);
        }
        catch (Exception ex)
        {
            // Log to Microsoft.Extensions.Logging
            m_logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

            // Extract context from the request
            var accountNo = context.User.FindFirst("AccountNo")?.Value;
            var userName = context.User.Identity?.Name;
            var url = context.Request.Path + context.Request.QueryString;
            var ipAddress = GetClientIpAddress(context);
            var isApi = context.Request.Path.StartsWithSegments("/api");

            // Log to database
            try
            {
                await logService.LogExceptionAsync(
                    ex,
                    accountNo,
                    userName,
                    url,
                    ipAddress,
                    isApi ? EventLog.Api : EventLog.Web
                );
            }
            catch (Exception logEx)
            {
                // If logging to database fails, at least log to console
                m_logger.LogError(logEx, "Failed to log exception to database");
            }

            // Re-throw for standard error handling
            throw;
        }
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        // Try to get IP from X-Forwarded-For header (for load balancers/proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs, take the first one (client IP)
            var ip = forwardedFor.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip))
                return ip;
        }

        // Try X-Real-IP header (nginx)
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        // Fall back to direct connection IP
        return context.Connection.RemoteIpAddress?.ToString();
    }
}

/// <summary>
/// Extension methods for registering the exception logging middleware.
/// </summary>
public static class ExceptionLoggingMiddlewareExtensions
{
    /// <summary>
    /// Adds exception logging middleware to the application pipeline.
    /// Should be placed early in the pipeline, before UseExceptionHandler.
    /// </summary>
    public static IApplicationBuilder UseExceptionLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionLoggingMiddleware>();
    }
}
