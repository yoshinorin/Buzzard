using Buzzard.Extensions;
using Buzzard.Models;

namespace Buzzard.Middleware;

public class FirewallMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FirewallMiddleware> _logger;
    private readonly FirewallConfig _config;

    public FirewallMiddleware(
        RequestDelegate next,
        ILogger<FirewallMiddleware> logger,
        FirewallConfig config)
    {
        _next = next;
        _logger = logger;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.GetClientIpAddress();
        var path = context.Request.Path.Value ?? "";
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var method = context.Request.Method;

        if (IsPathBlocked(path))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Forbidden");
            return;
        }

        await _next(context);
    }

    private bool IsPathBlocked(string path)
    {
        if (IsPathAllowed(path))
        {
            return false;
        }

        if (IsPathDenied(path))
        {
            return true;
        }

        return false;
    }

    private bool IsPathAllowed(string path)
    {
        var allow = _config.Path.Allow;
        return allow.Contains.Any(pattern => path.Contains(pattern, StringComparison.OrdinalIgnoreCase)) ||
               allow.StartsWith.Any(pattern => path.StartsWith(pattern, StringComparison.OrdinalIgnoreCase)) ||
               allow.EndsWith.Any(pattern => path.EndsWith(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsPathDenied(string path)
    {
        var deny = _config.Path.Deny;
        return deny.Contains.Any(pattern => path.Contains(pattern, StringComparison.OrdinalIgnoreCase)) ||
               deny.StartsWith.Any(pattern => path.StartsWith(pattern, StringComparison.OrdinalIgnoreCase)) ||
               deny.EndsWith.Any(pattern => path.EndsWith(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
