using System.Net;
using Buzzard.Extensions;

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
        var clientIp = context.GetClientIpAddress();
        var path = context.Request.Path.Value ?? "";
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var method = context.Request.Method;

        if (_blockedPaths.Any(blockedPath => path.StartsWith(blockedPath, StringComparison.OrdinalIgnoreCase)))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Forbidden");
            return;
        }

        await _next(context);
    }

}
