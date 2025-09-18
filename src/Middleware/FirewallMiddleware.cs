using Buzzard.Extensions;
using Buzzard.Services;

namespace Buzzard.Middleware;

public class FirewallMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FirewallMiddleware> _logger;
    private readonly IPathValidator _pathValidator;
    private readonly IUserAgentValidator _userAgentValidator;

    public FirewallMiddleware(
        RequestDelegate next,
        ILogger<FirewallMiddleware> logger,
        IPathValidator pathValidator,
        IUserAgentValidator userAgentValidator)
    {
        _next = next;
        _logger = logger;
        _pathValidator = pathValidator;
        _userAgentValidator = userAgentValidator;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.GetClientIpAddress();
        var path = context.Request.Path.Value ?? "";
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var method = context.Request.Method;

        if (_pathValidator.IsPathBlocked(path))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Forbidden");
            return;
        }

        if (_userAgentValidator.IsUserAgentBlocked(userAgent))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Forbidden");
            return;
        }

        await _next(context);
    }
}
