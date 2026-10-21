using System.Diagnostics;
using Buzzard.Extensions;
using Buzzard.Services;

namespace Buzzard.Middleware;

public class FirewallMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FirewallMiddleware> _logger;
    private readonly IPathValidator _pathValidator;
    private readonly IUserAgentValidator _userAgentValidator;
    private readonly RequestLogScope _requestLogScope;

    public FirewallMiddleware(
        RequestDelegate next,
        ILogger<FirewallMiddleware> logger,
        IPathValidator pathValidator,
        IUserAgentValidator userAgentValidator,
        RequestLogScope requestLogScope)
    {
        _next = next;
        _logger = logger;
        _pathValidator = pathValidator;
        _userAgentValidator = userAgentValidator;
        _requestLogScope = requestLogScope;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        var request = context.Extract();
        var ip = request.Ip;
        var method = request.Method;
        var path = RequestLogScope.Truncate(request.Path);
        var userAgent = RequestLogScope.Truncate(request.UserAgent);

        var pathRule = _pathValidator.FindBlockingRule(request.Path);
        if (pathRule is not null)
        {
            using (_requestLogScope.Begin(_logger, context, request, ("Rule", pathRule)))
            {
                _logger.LogWarning("Request blocked by Path: {Ip} {Method} {Path} {UserAgent}", ip, method, path, userAgent);
            }
            await RejectAsync(context);
            return;
        }

        var userAgentRule = _userAgentValidator.IsPathExempt(request.Path) ? null : _userAgentValidator.FindBlockingRule(request.UserAgent);
        if (userAgentRule is not null)
        {
            using (_requestLogScope.Begin(_logger, context, request, ("Rule", userAgentRule)))
            {
                _logger.LogWarning("Request blocked by UserAgent: {Ip} {Method} {Path} {UserAgent}", ip, method, path, userAgent);
            }
            await RejectAsync(context);
            return;
        }

        await _next(context);

        var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        var forwarderError = context.GetForwarderErrorFeature();
        if (forwarderError is not null)
        {
            using (_requestLogScope.Begin(_logger, context, request, ("StatusCode", context.Response.StatusCode), ("ElapsedMs", elapsedMs), ("ForwarderError", forwarderError.Error.ToString())))
            {
                _logger.LogError(forwarderError.Exception, "Request forwarding failed: {Ip} {Method} {Path} {UserAgent}", ip, method, path, userAgent);
            }
            return;
        }

        using (_requestLogScope.Begin(_logger, context, request, ("StatusCode", context.Response.StatusCode), ("ElapsedMs", elapsedMs)))
        {
            _logger.LogInformation("Request allowed: {Ip} {Method} {Path} {UserAgent}", ip, method, path, userAgent);
        }
    }

    private static Task RejectAsync(HttpContext context)
    {
        context.Response.StatusCode = 403;
#if DEBUG
        return context.Response.WriteAsync("Forbidden");
#else
        return Task.CompletedTask;
#endif
    }
}
