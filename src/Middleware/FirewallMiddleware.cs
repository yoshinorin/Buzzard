using System.Net;

namespace Buzzard.Middleware;

public class FirewallMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FirewallMiddleware> _logger;
    private readonly List<string> _blockedPaths;

    public FirewallMiddleware(
        RequestDelegate next,
        ILogger<FirewallMiddleware> logger,
        List<string> blockedPaths)
    {
        _next = next;
        _logger = logger;
        _blockedPaths = blockedPaths;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = GetClientIpAddress(context);
        var path = context.Request.Path.Value ?? "";
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var method = context.Request.Method;

        _logger.LogDebug("Request: {Method} {Path} from {ClientIp} - UserAgent: {UserAgent}", method, path, clientIp, userAgent);

        if (_blockedPaths.Any(blockedPath => path.StartsWith(blockedPath, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Blocked request to path: {Path} from IP: {ClientIp} - UserAgent: {UserAgent}", path, clientIp, userAgent);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Forbidden");
            return;
        }

        await _next(context);
    }

    private static IPAddress GetClientIpAddress(HttpContext context)
    {
        // Try X-Forwarded-For header first (for proxy scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var firstIp = forwardedFor.Split(',')[0].Trim();
            if (IPAddress.TryParse(firstIp, out var parsedIp))
                return parsedIp;
        }

        // Fallback to connection remote IP
        return context.Connection.RemoteIpAddress ?? IPAddress.Loopback;
    }
}
