using System.Net;
using Buzzard.Models;

namespace Buzzard.Extensions;

public static class HttpContextExtensions
{
    public static IPAddress GetClientIpAddress(this HttpContext context)
    {
        // Try X-Forwarded-For header first (for proxy scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var firstIp = forwardedFor.Split(',')[0].Trim();
            if (IPAddress.TryParse(firstIp, out var parsedIp))
            {
                return parsedIp;
            }
        }

        // Fallback to connection remote IP
        return context.Connection.RemoteIpAddress ?? IPAddress.Loopback;
    }

    public static Request Extract(this HttpContext context)
    {
        var clientIp = context.GetClientIpAddress().ToString();
        var path = context.Request.Path.Value ?? "";
        var queryString = context.Request.QueryString.ToString();
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var method = context.Request.Method;

        return new Request(method: method, path: path, ip: clientIp, queryString: queryString, userAgent: userAgent);
    }
}
