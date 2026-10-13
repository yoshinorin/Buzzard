using System.Net;
using Buzzard.Models;

namespace Buzzard.Extensions;

public static class HttpContextExtensions
{
    // Forwarded headers are resolved into RemoteIpAddress by ForwardedHeadersMiddleware only when sent from trusted proxies.
    public static IPAddress GetClientIpAddress(this HttpContext context)
    {
        return context.Connection.RemoteIpAddress ?? IPAddress.Loopback;
    }

    public static Request Extract(this HttpContext context)
    {
        var clientIp = context.GetClientIpAddress().ToString();
        var path = context.Request.Path.Value ?? "";
        var queryString = context.Request.QueryString.ToString();
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var method = context.Request.Method;
        var host = context.Request.Host.Value ?? "";

        return new Request(method: method, host: host, path: path, ip: clientIp, queryString: queryString, userAgent: userAgent);
    }
}
